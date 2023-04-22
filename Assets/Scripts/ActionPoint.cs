using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using IO.Swagger.Model;
//using WebSocketSharp;

namespace Base {
    public abstract class ActionPoint : MonoBehaviour {

        public GameObject ActionPoints;

        protected Vector3 offset;
        [System.NonSerialized]
        public int PuckCounter = 0;
        //public LineConnection ConnectionToParent;

        [System.NonSerialized]
        public IO.Swagger.Model.ActionPoint Data = new IO.Swagger.Model.ActionPoint(id: "", robotJoints: new List<IO.Swagger.Model.ProjectRobotJoints>(), orientations: new List<IO.Swagger.Model.NamedOrientation>(), position: new IO.Swagger.Model.Position(), actions: new List<IO.Swagger.Model.Action>(), name: "");
        
        [SerializeField]
        protected GameObject orientations;

        public virtual void ActionPointBaseUpdate(IO.Swagger.Model.BareActionPoint apData) {
            Data.Name = apData.Name;
            Data.Position = apData.Position;
            // update position and rotation based on received data from swagger
            transform.localPosition = GetScenePosition();
        }

        public virtual void InitAP(IO.Swagger.Model.ActionPoint apData, float size) {
            Debug.Assert(apData != null);
            Data = apData;

            //SelectorItem = SelectorMenu.Instance.CreateSelectorItem(this);
            transform.localPosition = GetScenePosition();
            SetSize(size);
            if (Data.Actions == null)
                Data.Actions = new List<IO.Swagger.Model.Action>();
            if (Data.Orientations == null)
                Data.Orientations = new List<NamedOrientation>();
            if (Data.RobotJoints == null)
                Data.RobotJoints = new List<ProjectRobotJoints>();
        }

        internal string GetFreeOrientationName() {
            int i = 1;
            bool hasFreeName;
            string freeName = "default";
            do {
                hasFreeName = true;
                if (OrientationNameExist(freeName) || JointsNameExist(freeName)) {
                    hasFreeName = false;
                }
                if (!hasFreeName)
                    freeName = "default_" + i++.ToString();
            } while (!hasFreeName);

            return freeName;
        }

        internal string GetFreeJointsName() {
            int i = 1;
            bool hasFreeName;
            string freeName = "default";
            do {
                hasFreeName = true;
                if (JointsNameExist(freeName) || OrientationNameExist(freeName)) {
                    hasFreeName = false;
                }
                if (!hasFreeName)
                    freeName = "default_" + i++.ToString();
            } while (!hasFreeName);

            return freeName;
        }

        public Dictionary<string, IO.Swagger.Model.Pose> GetPoses() {
            Dictionary<string, IO.Swagger.Model.Pose> poses = new Dictionary<string, IO.Swagger.Model.Pose>();
            foreach (IO.Swagger.Model.NamedOrientation orientation in Data.Orientations) {
                poses.Add(orientation.Id, new IO.Swagger.Model.Pose(orientation: orientation.Orientation, position: Data.Position));
            }
            return poses;
        }

        public List<IO.Swagger.Model.NamedOrientation> GetNamedOrientations() {
            return Data.Orientations;
        }

        public NamedOrientation GetNamedOrientationByName(string name) {
            foreach (NamedOrientation orientation in Data.Orientations)
                if (orientation.Name == name)
                    return orientation;
            throw new KeyNotFoundException("Orientation with name " + name + " not found.");
        }

        public NamedOrientation GetNamedOrientation(string id) {
            foreach (NamedOrientation orientation in Data.Orientations)
                if (orientation.Id == id)
                    return orientation;
            throw new KeyNotFoundException("Orientation with id " + id + " not found.");
        }

        public NamedOrientation GetFirstOrientation() {
            try {
                if (Data.Orientations.Count == 0) {
                    if (!string.IsNullOrEmpty(Data.Parent)) {
                        ActionPoint parent = ProjectManager.Instance.GetActionPoint(Data.Parent);
                        return parent.GetFirstOrientation();
                    }

                } else {
                    return Data.Orientations[0];
                }
            } catch (KeyNotFoundException) {
                
            }
            throw new ItemNotFoundException("No orientation");
        }

        public NamedOrientation GetFirstOrientationFromDescendants() {
            List<ActionPoint> descendantActionPoints = new List<ActionPoint>();
            foreach (Transform t in ActionPoints.transform) {
                ActionPoint ap = t.GetComponent<ActionPoint>();
                if (ap.Data.Orientations.Count > 0)
                    return ap.Data.Orientations[0];
                descendantActionPoints.Add(ap);
            }
            foreach (ActionPoint ap in descendantActionPoints) {
                try {
                    return ap.GetFirstOrientationFromDescendants();
                } catch (ItemNotFoundException) {

                }
            }
            
            throw new ItemNotFoundException("No orientation");
        }

        public IO.Swagger.Model.Pose GetDefaultPose() {
            foreach (IO.Swagger.Model.NamedOrientation orientation in Data.Orientations) {
                if (orientation.Id == "default")
                    return new IO.Swagger.Model.Pose(position: Data.Position, orientation: orientation.Orientation);
            }
            throw new ItemNotFoundException();            
        }


        public void DeleteAP(bool removeFromList = true) {
            // Remove all actions of this action point
            //RemoveActions();
            //RemoveConnectionToParent();
            RemoveOrientations();

            // Remove this ActionPoint reference from parent ActionObject list
            if (removeFromList) // to allow remove all AP in foreach
                ProjectManager.Instance.ActionPoints.Remove(this.Data.Id);

            DestroyObject();
            Destroy(gameObject);
        }

        public void DestroyObject() {
            Destroy(gameObject);
        }

        //private void RemoveConnectionToParent() {
        //    // Remove connections to parent
        //    if (ConnectionToParent != null && ConnectionToParent.gameObject != null) {
        //        // remove connection from connectinos manager
        //        SceneManager.Instance.AOToAPConnectionsManager.RemoveConnection(ConnectionToParent);
        //        // destroy connection gameobject
        //        Destroy(ConnectionToParent.gameObject);
        //    }
        //}

        public virtual bool ProjectInteractable() {
            return GameManager.Instance.GetGameState() == GameManager.GameStateEnum.ProjectEditor;
        }

        public abstract Vector3 GetScenePosition();
        public abstract Quaternion GetSceneOrientation();

        //public void RemoveActions() {
        //    // Remove all actions of this action point
        //    foreach (string actionUUID in Actions.Keys.ToList<string>()) {
        //        RemoveAction(actionUUID);
        //    }
        //    Actions.Clear();
        //}

        //public void RemoveAction(string action_id) {
        //    Actions[action_id].DeleteAction();
        //}

        

        public virtual void ActivateForGizmo(string layer) {
            gameObject.layer = LayerMask.NameToLayer(layer);
        }

        public Transform GetTransform() {
            return transform;
        }

        /// <summary>
        /// Updates actions of ActionPoint and ProjectActionPoint received from server.
        /// </summary>
        /// <param name="projectActionPoint"></param>
        /// <returns></returns>
        public virtual (List<string>, Dictionary<string, string>) UpdateActionPoint(IO.Swagger.Model.ActionPoint projectActionPoint) {
            //if (Data.Parent != projectActionPoint.Parent) {
            //    ChangeParent(projectActionPoint.Parent);
            //}
            Data = projectActionPoint;
            transform.localPosition = GetScenePosition();            
            transform.localRotation = GetSceneOrientation();
            List<string> currentA = new List<string>();

            foreach (NamedOrientation orientation in Data.Orientations) {
                UpdateOrientation(orientation);
            }
            // Connections between actions (action -> output --- input <- action2)
            Dictionary<string, string> connections = new Dictionary<string, string>();
            //if (projectActionPoint.Actions != null) {
            //    //update actions
            //    foreach (IO.Swagger.Model.Action projectAction in projectActionPoint.Actions) {

            //        // if action exist, just update it, otherwise create new
            //        if (!Actions.TryGetValue(projectAction.Id, out Action action)) {
            //            try {
            //                action = ProjectManager.Instance.SpawnAction(projectAction, this);
            //            } catch (RequestFailedException ex) {
            //                Debug.LogError(ex);
            //                continue;
            //            }
            //        }
            //        // updates name of the action
            //        action.ActionUpdateBaseData(DataHelper.ActionToBareAction(projectAction));
            //        // updates parameters of the action
            //        action.ActionUpdate(projectAction);

            //        // Add current connection from the server, we will only map the outputs
                    

            //        // local list of all actions for current action point
            //        currentA.Add(projectAction.Id);
            //    }
            //}        
 
            return (currentA, connections);
        }

        //private void ChangeParent(string parentId) {
        //    if (parentId == null || parentId == "") {
        //        RemoveConnectionToParent();
        //        Parent = null;
        //        Data.Parent = "";
        //        transform.parent = ProjectManager.Instance.ActionPointsOrigin.transform;
        //        transform.localRotation = Quaternion.identity;
        //        return;
        //    } else {
        //        try {
        //            IActionPointParent actionPointParent = ProjectManager.Instance.GetActionPointParent(parentId);
        //            Parent = actionPointParent;
        //            Data.Parent = parentId;
        //            transform.parent = actionPointParent.GetTransform();
        //            RemoveConnectionToParent();
        //            SetConnectionToParent(Parent);
        //        } catch (KeyNotFoundException ex) {
        //            Debug.LogError(ex);
        //        }
        //    }
        //}

        public bool OrientationNameExist(string name) {
            try {
                GetOrientationByName(name);
                return true;
            } catch (KeyNotFoundException ex) {
                return false;
            } 
        } 

        public bool JointsNameExist(string name) {
            try {
                GetJointsByName(name);
                return true;
            } catch (KeyNotFoundException ex) {
                return false;
            } 
        } 

        /// <summary>
        /// Returns orientation with id or throws KeyNotFoundException
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IO.Swagger.Model.NamedOrientation GetOrientation(string id) {
            foreach (IO.Swagger.Model.NamedOrientation orientation in Data.Orientations) {
                if (orientation.Id == id)
                    return orientation;
            }
            throw new KeyNotFoundException("Orientation with id " + id + " not found");
        }

        /// <summary>
        /// Returns joints with id or throws KeyNotFoundException
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IO.Swagger.Model.ProjectRobotJoints GetJoints(string id) {
            foreach (ProjectRobotJoints joints in Data.RobotJoints) {
                if (joints.Id == id)
                    return joints;
            }
            throw new KeyNotFoundException("Joints with id " + id + " not found");
        }

        /// <summary>
        /// Returns joints with name or throws KeyNotFoundException
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IO.Swagger.Model.ProjectRobotJoints GetJointsByName(string name) {
            foreach (ProjectRobotJoints joints in Data.RobotJoints) {
                if (joints.Name == name)
                    return joints;
            }
            throw new KeyNotFoundException("Joints with name " + name + " not found");
        }


        /// <summary>
        /// Returns joints with name or throws KeyNotFoundException
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IO.Swagger.Model.NamedOrientation GetOrientationByName(string name) {
            foreach (NamedOrientation orientation in Data.Orientations) {
                if (orientation.Name == name)
                    return orientation;
            }
            throw new KeyNotFoundException("Orientation with name " + name + " not found");
        }


        public virtual void UpdateOrientation(NamedOrientation orientation) {
            NamedOrientation originalOrientation = GetOrientation(orientation.Id);
            originalOrientation.Orientation = orientation.Orientation;
            try {
                //APOrientation orientationArrow = GetOrientationVisual(orientation.Id);
               // orientationArrow.SetOrientation(orientation.Orientation);
            } catch (KeyNotFoundException) {
                AddNewOrientationVisual(orientation);
            }
            BaseUpdateOrientation(originalOrientation, orientation);
        }

        public virtual void AddOrientation(NamedOrientation orientation) {
            Data.Orientations.Add(orientation);
            AddNewOrientationVisual(orientation);
        }


        public void BaseUpdateOrientation(NamedOrientation orientation) {
            NamedOrientation originalOrientation = GetOrientation(orientation.Id);
            BaseUpdateOrientation(originalOrientation, orientation);
            //GetOrientationVisual(orientation.Id).SelectorItem.SetText(orientation.Name);
        }

        public void BaseUpdateOrientation(NamedOrientation originalOrientation, NamedOrientation orientation) {
            originalOrientation.Name = orientation.Name;
        }

        public void RemoveOrientations() {
            foreach (NamedOrientation o in Data.Orientations) {
                RemoveOrientationVisual(o.Id);
            }
            Data.Orientations.Clear();
        }
        
        public void RemoveOrientation(string id) {
            int i = 0;
            foreach (NamedOrientation o in Data.Orientations) {
                if (o.Id == id) {
                    Data.Orientations.RemoveAt(i);
                    break;
                    ;
                }
                ++i;
            }
            RemoveOrientationVisual(id);
        }

        private void RemoveOrientationVisual(string id) {
            try {
                //APOrientation o = GetOrientationVisual(id);
                //o.DestroyObject();
            } catch (KeyNotFoundException) {
                // älready destroyed..
            }          

        }

        public void UpdateJoints(ProjectRobotJoints joints) {
            ProjectRobotJoints originalJoints = GetJoints(joints.Id);
            originalJoints.Joints = joints.Joints;
            BaseUpdateJoints(originalJoints, joints);
        }

        public void BaseUpdateJoints(ProjectRobotJoints joints) {
            ProjectRobotJoints originalJoints = GetJoints(joints.Id);
            BaseUpdateJoints(originalJoints, joints);
        }

        public void BaseUpdateJoints(ProjectRobotJoints originalJoints, ProjectRobotJoints joints) {
            originalJoints.Name = joints.Name;
            originalJoints.IsValid = joints.IsValid;
            originalJoints.RobotId = joints.RobotId;
        }

        public void AddJoints(ProjectRobotJoints joints) {
            Data.RobotJoints.Add(joints);
        }

        public void RemoveJoints(string id) {
            int i = 0;
            foreach (ProjectRobotJoints joints in Data.RobotJoints) {
                if (joints.Id == id) {
                    Data.RobotJoints.RemoveAt(i);
                    return;
                }
                ++i;
            }
        }
        public GameObject GetGameObject() {
            return gameObject;
        }

        public abstract void SetSize(float size);

        public void UpdateOrientationsVisuals(bool visible) {
            foreach (Transform transform in orientations.transform) {
                transform.gameObject.SetActive(visible);
            }
        }

        private void AddNewOrientationVisual(NamedOrientation orientation) {
            //APOrientation apOrientation = Instantiate(ActionsManager.Instance.ActionPointOrientationPrefab, orientations.transform).GetComponent<APOrientation>();
            //apOrientation.ActionPoint = this;
            //apOrientation.SetOrientation(orientation.Orientation);
            //apOrientation.OrientationId = orientation.Id;
            //apOrientation.SelectorItem = SelectorMenu.Instance.CreateSelectorItem(apOrientation);
        }

        internal async Task<bool> ShowOrientationDetailMenu(string orientationId) {
            //if (await ActionPointAimingMenu.Instance.Show(this, true)) {
            //    ActionPointAimingMenu.Instance.OpenDetailMenu(GetOrientation(orientationId));
            //    return true;
            //}
            return false;
        }

        //public abstract void HighlightAP(bool highlight);


        public void ResetPosition() {
            transform.localPosition = GetScenePosition();
            transform.localRotation = GetSceneOrientation();
        }

        public string GetName()
        {
            return Data.Name;
        }

        public string GetId() {
            return Data.Id;
        }

        public bool AnyOrientation() {
            return Data.Orientations.Count > 0;
        }

        public bool AnyJoints() {
            return Data.RobotJoints.Count > 0;
        }

        public Transform GetSpawnPoint() {
            return ActionPoints.transform;
        }
    }
}
