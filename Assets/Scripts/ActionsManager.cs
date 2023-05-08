using System.Collections.Generic;
using System;
using IO.Swagger.Model;
using Assets.Scripts.ARClasses;

namespace Base
{
    //This class was taken from AREditor and modified for the needs of this application. (https://github.com/robofit/arcor2_areditor)
    public class ActionsManager : Singleton<ActionsManager>
    {
        private Dictionary<string, ActionObjectMetadata> actionObjectsMetadata = new Dictionary<string, ActionObjectMetadata>();

        public bool ActionsReady, ActionObjectsLoaded;

        public Dictionary<string, ActionObjectMetadata> ActionObjectsMetadata
        {
            get => actionObjectsMetadata; set => actionObjectsMetadata = value;
        }
        private void Awake()
        {
            ActionsReady = false;
            ActionObjectsLoaded = false;
        }

        private void Start()
        {
            Init();
            WebsocketManager.Instance.OnDisconnectEvent += OnDisconnected;
            WebsocketManager.Instance.OnObjectTypeAdded += ObjectTypeAdded;
            WebsocketManager.Instance.OnObjectTypeRemoved += ObjectTypeRemoved;
            WebsocketManager.Instance.OnObjectTypeUpdated += ObjectTypeUpdated;
        }

        private void OnDisconnected(object sender, EventArgs args)
        {
            Init();
        }

        private void Update()
        {
            if (!ActionsReady && ActionObjectsLoaded)
            {
                foreach (ActionObjectMetadata ao in ActionObjectsMetadata.Values)
                {
                    if (!ao.Disabled && !ao.ActionsLoaded)
                    {
                        return;
                    }
                }
                ActionsReady = true;
                enabled = false;
            }
        }

        public void Init()
        {
            actionObjectsMetadata.Clear();
            ActionsReady = false;
            ActionObjectsLoaded = false;
        }

        public void ObjectTypeRemoved(object sender, StringListEventArgs type)
        {
            foreach (string item in type.Data)
            {
                if (actionObjectsMetadata.ContainsKey(item))
                {
                    actionObjectsMetadata.Remove(item);
                }
            }
        }

        public void ObjectTypeAdded(object sender, ObjectTypesEventArgs args)
        {
            ActionsReady = false;
            enabled = true;
            foreach (ObjectTypeMeta obj in args.ObjectTypes)
            {
                ActionObjectMetadata m = new ActionObjectMetadata(meta: obj);
                m.ActionsLoaded = true;
                m.Robot = IsDescendantOfType("Robot", m);
                m.Camera = IsDescendantOfType("Camera", m);
                m.CollisionObject = IsDescendantOfType("VirtualCollisionObject", m);
                actionObjectsMetadata.Add(obj.Type, m);
            }
        }

        public void ObjectTypeUpdated(object sender, ObjectTypesEventArgs args)
        {
            ActionsReady = false;
            enabled = true;
            foreach (ObjectTypeMeta obj in args.ObjectTypes)
            {
                if (actionObjectsMetadata.TryGetValue(obj.Type, out ActionObjectMetadata actionObjectMetadata))
                {
                    actionObjectMetadata.Update(obj);
                    actionObjectMetadata.ActionsLoaded = true;
                    foreach (ActionObject updatedObj in SceneManager.Instance.GetAllObjectsOfType(obj.Type))
                    {
                        updatedObj.UpdateModel();
                    }
                }
                else
                {
                    Notifications.Instance.ShowNotification("Update of object types failed", "Server trying to update non-existing object!");
                }
            }
        }

        public void UpdateObjects(List<IO.Swagger.Model.ObjectTypeMeta> newActionObjectsMetadata)
        {
            ActionsReady = false;
            actionObjectsMetadata.Clear();
            foreach (IO.Swagger.Model.ObjectTypeMeta metadata in newActionObjectsMetadata)
            {
                ActionObjectMetadata m = new ActionObjectMetadata(meta: metadata);
                m.ActionsLoaded = true;
                actionObjectsMetadata.Add(metadata.Type, m);
            }
            foreach (KeyValuePair<string, ActionObjectMetadata> kv in actionObjectsMetadata)
            {
                kv.Value.Robot = IsDescendantOfType("Robot", kv.Value);
                kv.Value.Camera = IsDescendantOfType("Camera", kv.Value);
                kv.Value.CollisionObject = IsDescendantOfType("VirtualCollisionObject", kv.Value);
            }
            enabled = true;
            ActionObjectsLoaded = true;
        }

        private bool IsDescendantOfType(string type, ActionObjectMetadata actionObjectMetadata)
        {
            if (actionObjectMetadata.Type == type)
                return true;
            if (actionObjectMetadata.Type == "Generic")
                return false;
            foreach (KeyValuePair<string, ActionObjectMetadata> kv in actionObjectsMetadata)
            {
                if (kv.Key == actionObjectMetadata.Base)
                {
                    return IsDescendantOfType(type, kv.Value);
                }
            }
            return false;
        }
    }
}

