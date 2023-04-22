using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;

using IO.Swagger.Model;
using System.Linq;
using Assets.Scripts.ARClasses;

namespace Base {
    /// <summary>
    /// Takes care of currently opened project. Provides methods for manipuation with project.
    /// </summary>
    public class ProjectManager : Base.Singleton<ProjectManager> {

        #region fields
        /// <summary>
        /// Opened project metadata
        /// </summary>
        public IO.Swagger.Model.Project ProjectMeta = null;
        /// <summary>
        /// All action points in scene
        /// </summary>
        public Dictionary<string, ActionPoint> ActionPoints = new Dictionary<string, ActionPoint>();
        /// <summary>
        /// All logic items (i.e. connections of actions) in project
        /// </summary>
        public Dictionary<string, LogicItem> LogicItems = new Dictionary<string, LogicItem>();
        /// <summary>
        /// All Project parameters <id, instance>
        /// </summary>
        public List<IO.Swagger.Model.ProjectParameter> ProjectParameters = new List<ProjectParameter>();
        /// <summary>
        /// Spawn point for global action points
        /// </summary>
        public GameObject ActionPointsOrigin;
        /// <summary>
        /// Prefab for project elements
        /// </summary>
        public GameObject ConnectionPrefab, ActionPointPrefab, PuckPrefab, StartPrefab, EndPrefab;
        /// <summary>
        /// Indicates if action point orientations should be visible for given project
        /// </summary>
        //public bool APOrientationsVisible;
        /// <summary>
        /// Holds current diameter of action points
        /// </summary>
        public float APSize = 0.2f;
        /// <summary>
        /// Indicates if project is loaded
        /// </summary>
        public bool Valid = false;
        /// <summary>
        /// Indicates if editation of project is allowed.
        /// </summary>
        //public bool AllowEdit = false;
        /// <summary>
        /// Indicates if project was changed since last save
        /// </summary>
        private bool projectChanged;
        /// <summary>
        /// Flag which indicates whether project changed event should be trigered during Update()
        /// </summary>
        private bool updateProject = false;
        /// <summary>
        /// Public setter for project changed property. Invokes OnProjectChanged event with each change and
        /// OnProjectSavedSatusChanged when projectChanged value differs from original value (i.e. when project
        /// was not changed and now it is and vice versa) 
        /// </summary>
        public bool ProjectChanged {
            get => projectChanged;
            set {
                bool origVal = projectChanged;
                projectChanged = value;
                if (!Valid)
                    return;
                OnProjectChanged?.Invoke(this, EventArgs.Empty);
                if (origVal != value) {
                    OnProjectSavedSatusChanged?.Invoke(this, EventArgs.Empty);
                }
            } 
        }

        /// <summary>
        /// Invoked when project loaded
        /// </summary>
        public event EventHandler OnLoadProject;
        /// <summary>
        /// Invoked when project changed
        /// </summary>
        public event EventHandler OnProjectChanged;
        /// <summary>
        /// Invoked when project saved
        /// </summary>
        public event EventHandler OnProjectSaved;
        /// <summary>
        /// Invoked when projectChanged value differs from original value (i.e. when project
        /// was not changed and now it is and vice versa) 
        /// </summary>
        public event EventHandler OnProjectSavedSatusChanged;
        /// <summary>
        /// Indicates whether there is any object with available action in the scene
        /// </summary>
        public bool AnyAvailableAction;

        public event AREditorEventArgs.ActionPointEventHandler OnActionPointAddedToScene;
        //public event AREditorEventArgs.ActionEventHandler OnActionAddedToScene;

        public event AREditorEventArgs.ActionPointOrientationEventHandler OnActionPointOrientationAdded;
        public event AREditorEventArgs.ActionPointOrientationEventHandler OnActionPointOrientationUpdated;
        public event AREditorEventArgs.ActionPointOrientationEventHandler OnActionPointOrientationBaseUpdated;
        public event AREditorEventArgs.StringEventHandler OnActionPointOrientationRemoved;

        #endregion

        /// <summary>
        /// Initialization of projet manager
        /// </summary>
        private void Start() {
            WebsocketManager.Instance.OnLogicItemAdded += OnLogicItemAdded;
            WebsocketManager.Instance.OnLogicItemRemoved += OnLogicItemRemoved;
            WebsocketManager.Instance.OnLogicItemUpdated += OnLogicItemUpdated;
            WebsocketManager.Instance.OnProjectBaseUpdated += OnProjectBaseUpdated;

            WebsocketManager.Instance.OnActionPointAdded += OnActionPointAdded;
            WebsocketManager.Instance.OnActionPointRemoved += OnActionPointRemoved;
            WebsocketManager.Instance.OnActionPointUpdated += OnActionPointUpdated;
            WebsocketManager.Instance.OnActionPointBaseUpdated += OnActionPointBaseUpdated;

            WebsocketManager.Instance.OnActionPointOrientationAdded += OnActionPointOrientationAddedCallback;
            WebsocketManager.Instance.OnActionPointOrientationUpdated += OnActionPointOrientationUpdatedCallback;
            WebsocketManager.Instance.OnActionPointOrientationBaseUpdated += OnActionPointOrientationBaseUpdatedCallback;
            WebsocketManager.Instance.OnActionPointOrientationRemoved += OnActionPointOrientationRemovedCallback;

            WebsocketManager.Instance.OnActionPointJointsAdded += OnActionPointJointsAdded;
            WebsocketManager.Instance.OnActionPointJointsUpdated += OnActionPointJointsUpdated;
            WebsocketManager.Instance.OnActionPointJointsBaseUpdated += OnActionPointJointsBaseUpdated;
            WebsocketManager.Instance.OnActionPointJointsRemoved += OnActionPointJointsRemoved;

            WebsocketManager.Instance.OnProjectParameterAdded += OnProjectParameterAdded;
            WebsocketManager.Instance.OnProjectParameterUpdated += OnProjectParameterUpdated;
            WebsocketManager.Instance.OnProjectParameterRemoved += OnProjectParameterRemoved;
        }

        private void OnProjectParameterRemoved(object sender, ProjectParameterEventArgs args) {
            ProjectParameters.Remove(args.ProjectParameter);
            ProjectChanged = true;
        }

        private void OnProjectParameterUpdated(object sender, ProjectParameterEventArgs args) {
            ProjectParameters.RemoveAll(c => c.Id == args.ProjectParameter.Id);
            ProjectParameters.Add(args.ProjectParameter);
            ProjectChanged = true;
        }

        private void OnProjectParameterAdded(object sender, ProjectParameterEventArgs args) {
            ProjectParameters.Add(args.ProjectParameter);
            ProjectChanged = true;
        }

        private void OnActionPointJointsRemoved(object sender, StringEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPointWithJoints(args.Data);
                actionPoint.RemoveJoints(args.Data);
                updateProject = true;
                OnProjectChanged?.Invoke(this, EventArgs.Empty);
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to remove action point joints", ex.Message);
                return;
            }
        }

        private void OnActionPointJointsBaseUpdated(object sender, RobotJointsEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPointWithJoints(args.Data.Id);
                actionPoint.BaseUpdateJoints(args.Data);
                updateProject = true;
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to update action point joints", ex.Message);
                return;
            }
        }

        private void OnActionPointJointsUpdated(object sender, RobotJointsEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPointWithJoints(args.Data.Id);
                actionPoint.UpdateJoints(args.Data);
                updateProject = true;
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to update action point joints", ex.Message);
                return;
            }
        }

        private void OnActionPointJointsAdded(object sender, RobotJointsEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPoint(args.ActionPointId);
                actionPoint.AddJoints(args.Data);
                updateProject = true;
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to add action point joints", ex.Message);
                return;
            }
        }

        private void OnActionPointBaseUpdated(object sender, BareActionPointEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPoint(args.ActionPoint.Id);
                actionPoint.ActionPointBaseUpdate(args.ActionPoint);
                updateProject = true;
            } catch (KeyNotFoundException ex) {
                Debug.Log("Action point " + args.ActionPoint.Id + " not found!");
                Notifications.Instance.ShowNotification("", "Action point " + args.ActionPoint.Id + " not found!");
                return;
            }
        }

        private void OnActionPointOrientationBaseUpdatedCallback(object sender, ActionPointOrientationEventArgs args) {
            try {
                ActionPoint actionPoint = ProjectManager.Instance.GetActionPointWithOrientation(args.Data.Id);
                actionPoint.BaseUpdateOrientation(args.Data);

                updateProject = true;
                OnActionPointOrientationBaseUpdated?.Invoke(this, args);
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to update action point orientation", ex.Message);
                return;
            }
        }

        private void OnActionPointOrientationRemovedCallback(object sender, StringEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPointWithOrientation(args.Data);
                actionPoint.RemoveOrientation(args.Data);
                updateProject = true;
                OnActionPointOrientationRemoved?.Invoke(this, args);
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to remove action point orientation", ex.Message);
                return;
            }
        }

        private void OnActionPointOrientationUpdatedCallback(object sender, ActionPointOrientationEventArgs args) {
            try {
                ActionPoint actionPoint = ProjectManager.Instance.GetActionPointWithOrientation(args.Data.Id);
                actionPoint.UpdateOrientation(args.Data);
                updateProject = true;
                OnActionPointOrientationUpdated?.Invoke(this, args);
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to update action point orientation", ex.Message);
                return;
            }
        }

        private void OnActionPointOrientationAddedCallback(object sender, ActionPointOrientationEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPoint(args.ActionPointId);
                actionPoint.AddOrientation(args.Data);
                updateProject = true;
                OnActionPointOrientationAdded?.Invoke(this, args);
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to add action point orientation", ex.Message);
                return;
            }
        }

        private void OnActionPointRemoved(object sender, StringEventArgs args) {
            RemoveActionPoint(args.Data);
            updateProject = true;
        }

        private void OnActionPointUpdated(object sender, ProjectActionPointEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPoint(args.ActionPoint.Id);
                actionPoint.UpdateActionPoint(args.ActionPoint);
                updateProject = true;
                // TODO - update orientations, joints etc.
            } catch (KeyNotFoundException ex) {
                Debug.LogError("Action point " + args.ActionPoint.Id + " not found!");
                Notifications.Instance.ShowNotification("", "Action point " + args.ActionPoint.Id + " not found!");
                return;
            }
        }

        private void OnActionPointAdded(object sender, ProjectActionPointEventArgs data) {
            ActionPoint ap = null;
            if (data.ActionPoint.Parent == null || data.ActionPoint.Parent == "") {
                ap = SpawnActionPoint(data.ActionPoint);
            } else {
                try {
                    ap = SpawnActionPoint(data.ActionPoint);
                } catch (KeyNotFoundException ex) {
                    Debug.LogError(ex);
                }

            }
            
            updateProject = true;
        }

        private void OnProjectBaseUpdated(object sender, BareProjectEventArgs args) {
            if (GameManager.Instance.GetGameState() == GameManager.GameStateEnum.ProjectEditor) {
                SetProjectMeta(args.Project);
                updateProject = true;
            } 
        }

        /// <summary>
        /// Creates project from given json
        /// </summary>
        /// <param name="project">Project descriptoin in json</param>
        /// <param name="allowEdit">Sets if project is editable</param>
        /// <returns>True if project sucessfully created</returns>
        public async Task<bool> CreateProject(IO.Swagger.Model.Project project, bool allowEdit) {
            Debug.Assert(ActionsManager.Instance.ActionsReady);
            if (ProjectMeta != null)
                return false;

            SetProjectMeta(DataHelper.ProjectToBareProject(project));
            //AllowEdit = allowEdit;
            LoadSettings();
            AnyAvailableAction = false;
            foreach (ActionObject obj in SceneManager.Instance.ActionObjects.Values)
                if (obj.ActionObjectMetadata.ActionsMetadata.Count > 0) {
                    AnyAvailableAction = true;
                    break;
                }

            foreach (SceneObjectOverride objectOverrides in project.ObjectOverrides) {
                ActionObject actionObject = SceneManager.Instance.GetActionObject(objectOverrides.Id);
                foreach (IO.Swagger.Model.Parameter p in objectOverrides.Parameters) {
                    if (actionObject.TryGetParameterMetadata(p.Name, out ParameterMeta meta)) {
                        //Parameter parameter = new Parameter(meta, p.Value);
                        //actionObject.Overrides[p.Name] = parameter;
                    }
                    
                }
            }

            UpdateActionPoints(project);
            UpdateProjectParameters(project.Parameters);
            if (project.HasLogic) {
                //UpdateLogicItems(project.Logic);
            }
            if (project.Modified == System.DateTime.MinValue) { //new project, never saved
                projectChanged = true;
            } else if (project.IntModified == System.DateTime.MinValue) {
                ProjectChanged = false;
            } else {
                ProjectChanged = project.IntModified > project.Modified;
            }
            Valid = true;
            //OnLoadProject?.Invoke(this, EventArgs.Empty);
            //SetActionInputOutputVisibility(MainSettingsMenu.Instance.ConnectionsSwitch.IsOn());
            return true;
        }

        private void UpdateProjectParameters(List<ProjectParameter> projectParameters) {
            ProjectParameters.Clear();
            if (projectParameters == null)
                return;
            ProjectParameters.AddRange(projectParameters);
        }

        /// <summary>
        /// Destroys current project
        /// </summary>
        /// <returns>True if project successfully destroyed</returns>
        public bool DestroyProject() {
            Valid = false;
            ProjectMeta = null;
            foreach (ActionPoint ap in ActionPoints.Values) {
                ap.DeleteAP(false);
            }
            ActionPoints.Clear();
            //ConnectionManagerArcoro.Instance.Clear();
            LogicItems.Clear();
            ProjectParameters.Clear();
            ProjectionManager.Instance.DestroyProjection();
            return true;
        }

        /// <summary>
        /// Updates logic item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnLogicItemUpdated(object sender, LogicItemChangedEventArgs args) {
            
        }

        /// <summary>
        /// Removes logic item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnLogicItemRemoved(object sender, StringEventArgs args) {

        }

        /// <summary>
        /// Adds logic item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnLogicItemAdded(object sender, LogicItemChangedEventArgs args) {

        }

        /// <summary>
        /// Sets project metadata
        /// </summary>
        /// <param name="project"></param>
        public void SetProjectMeta(BareProject project) {
            if (ProjectMeta == null) {
                ProjectMeta = new Project(sceneId: "", id: "", name: "");
            }
            ProjectMeta.Id = project.Id;
            ProjectMeta.SceneId = project.SceneId;
            ProjectMeta.HasLogic = project.HasLogic;
            ProjectMeta.Description = project.Description;
            ProjectMeta.IntModified = project.IntModified;
            ProjectMeta.Modified = project.Modified;
            ProjectMeta.Name = project.Name;
            
        }

        /// <summary>
        /// Gets json describing project
        /// </summary>
        /// <returns></returns>
        public Project GetProject() {
            if (ProjectMeta == null)
                return null;
            Project project = ProjectMeta;
            project.ActionPoints = new List<IO.Swagger.Model.ActionPoint>();
            foreach (ActionPoint ap in ActionPoints.Values) {
                IO.Swagger.Model.ActionPoint projectActionPoint = ap.Data;
               
                project.ActionPoints.Add(projectActionPoint);
            }
            return project;
        }

        private void Update() {
            
            if (updateProject) {
                ProjectChanged = true;
                updateProject = false;
            }
        }

        /// <summary>
        /// Deactivates or activates all action points in scene for gizmo interaction.
        /// </summary>
        /// <param name="activate"></param>
        private void ActivateActionPointsForGizmo(bool activate) {
            if (activate) {
                //TODO: this should probably be Scene
                gameObject.layer = LayerMask.NameToLayer("GizmoRuntime");
                foreach (ActionPoint actionPoint in GetAllActionPoints()) {
                    actionPoint.ActivateForGizmo("GizmoRuntime");
                }
            } else {
                gameObject.layer = LayerMask.NameToLayer("Default");
                foreach (ActionPoint actionPoint in GetAllActionPoints()) {
                    actionPoint.ActivateForGizmo("Default");
                }
            }
        }

        /// <summary>
        /// Loads project settings from persistant storage
        /// </summary>
        public void LoadSettings() {
            //APOrientationsVisible = PlayerPrefsHelper.LoadBool("project/" + ProjectMeta.Id + "/APOrientationsVisibility", true);
            APSize = PlayerPrefsHelper.LoadFloat("project/" + ProjectMeta.Id + "/APSize", 0.2f);
        }

        /// <summary>
        /// Spawn action point into the project
        /// </summary>
        /// <param name="apData">Json describing action point</param>
        /// <param name="actionPointParent">Parent of action point. If null, AP is spawned as global.</param>
        /// <returns></returns>
        public ActionPoint SpawnActionPoint(IO.Swagger.Model.ActionPoint apData) {
            Debug.Assert(apData != null);
            GameObject AP;                         
            AP = Instantiate(ActionPointPrefab, ActionPointsOrigin.transform);
            
            AP.transform.localScale = new Vector3(1f, 1f, 1f);
            ActionPoint actionPoint = AP.GetComponent<ActionPoint>();
            actionPoint.InitAP(apData, APSize);
            ActionPoints.Add(actionPoint.Data.Id, actionPoint);
            OnActionPointAddedToScene?.Invoke(this, new ActionPointEventArgs(actionPoint));
            return actionPoint;
        }

        /// <summary>
        /// Updates action point GameObject in ActionObjects.ActionPoints dict based on the data present in IO.Swagger.Model.ActionPoint Data.
        /// </summary>
        /// <param name="project"></param>
        public void UpdateActionPoints(Project project) {
            List<string> currentAP = new List<string>();
            List<string> currentActions = new List<string>();
            Dictionary<string, List<IO.Swagger.Model.ActionPoint>> actionPointsWithParents = new Dictionary<string, List<IO.Swagger.Model.ActionPoint>>();
            // ordered list of already processed parents. This ensure that global APs are processed first,
            // then APs with action objects as a parents and then APs with already processed AP parents
            List<string> processedParents = new List<string> {
                "global"
            };
            foreach (IO.Swagger.Model.ActionPoint projectActionPoint in project.ActionPoints) {
                string parent = projectActionPoint.Parent;
                if (string.IsNullOrEmpty(parent)) {
                    parent = "global";
                }
                if (actionPointsWithParents.TryGetValue(parent, out List<IO.Swagger.Model.ActionPoint> projectActionPoints)) {
                    projectActionPoints.Add(projectActionPoint);
                } else {
                    List<IO.Swagger.Model.ActionPoint> aps = new List<IO.Swagger.Model.ActionPoint> {
                        projectActionPoint
                    };
                    actionPointsWithParents[parent] = aps;
                }
                // if parent is action object, we dont need to process it
                if (SceneManager.Instance.ActionObjects.ContainsKey(parent)) {
                    processedParents.Add(parent);
                }
            }

            for (int i = 0; i < processedParents.Count; ++i) {
                if (actionPointsWithParents.TryGetValue(processedParents[i], out List<IO.Swagger.Model.ActionPoint> projectActionPoints)) {
                    foreach (IO.Swagger.Model.ActionPoint projectActionPoint in projectActionPoints) {
                        // if action point exist, just update it
                        if (ActionPoints.TryGetValue(projectActionPoint.Id, out ActionPoint actionPoint)) {
                            actionPoint.ActionPointBaseUpdate(DataHelper.ActionPointToBareActionPoint(projectActionPoint));
                        }
                        // if action point doesn't exist, create new one
                        else {
                            actionPoint = SpawnActionPoint(projectActionPoint);

                        }

                        // update actions in current action point 
                        (List<string>, Dictionary<string, string>) updateActionsResult = actionPoint.UpdateActionPoint(projectActionPoint);
                        currentActions.AddRange(updateActionsResult.Item1);

                        //actionPoint.UpdatePositionsOfPucks();

                        currentAP.Add(actionPoint.Data.Id);

                        processedParents.Add(projectActionPoint.Id);
                    }
                }
                
            }

            // Remove deleted action points
            foreach (string actionPointId in GetAllActionPointsDict().Keys.ToList<string>()) {
                if (!currentAP.Contains(actionPointId)) {
                    RemoveActionPoint(actionPointId);
                }
            }
        }

        /// <summary>
        /// Removes all action points in project
        /// </summary>
        public void RemoveActionPoints() {
            List<ActionPoint> actionPoints = ActionPoints.Values.ToList();
            foreach (ActionPoint actionPoint in actionPoints) {
                actionPoint.DeleteAP();
            }
        }

        /// <summary>
        /// Gets action points based on its human readable name
        /// </summary>
        /// <param name="name">Human readable name of action point</param>
        /// <returns></returns>
        public ActionPoint GetactionpointByName(string name) {
            foreach (ActionPoint ap in ActionPoints.Values) {
                if (ap.Data.Name == name)
                    return ap;
            }
            throw new KeyNotFoundException("Action point " + name + " not found");
        }



        /// <summary>
        /// Destroys and removes references to action point of given Id.
        /// </summary>
        /// <param name="Id"></param>
        public void RemoveActionPoint(string Id) {
            if (ActionPoints.TryGetValue(Id, out ActionPoint actionPoint)) {
                actionPoint.DeleteAP();
            }
        }

        /// <summary>
        /// Returns action point of given Id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionPoint GetActionPoint(string id) {
            if (ActionPoints.TryGetValue(id, out ActionPoint actionPoint)) {
                return actionPoint;
            }

            throw new KeyNotFoundException("ActionPoint \"" + id + "\" not found!");
        }

        /// <summary>
        /// Returns all action points in the scene in a list [ActionPoint_object]
        /// </summary>
        /// <returns></returns>
        public List<ActionPoint> GetAllActionPoints() {
            return ActionPoints.Values.ToList();
        }

        /// <summary>
        /// Returns all action points in the scene in a list [ActionPoint_object]
        /// </summary>
        /// <returns></returns>
        public List<ActionPoint> GetAllGlobalActionPoints() {
            return (from ap in ActionPoints                    
                    select ap.Value).ToList();
        }

        /// <summary>
        /// Returns all action points in the scene in a dictionary [action_point_Id, ActionPoint_object]
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, ActionPoint> GetAllActionPointsDict() {
            return ActionPoints;
        }

        /// <summary>
        /// Returns joints with id or throws KeyNotFoundException
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IO.Swagger.Model.ProjectRobotJoints GetJoints(string id) {
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                try {
                    return actionPoint.GetJoints(id);
                } catch (KeyNotFoundException ex) { }
            }
            throw new KeyNotFoundException("Joints with id " + id + " not found");
        }

        public IO.Swagger.Model.ProjectRobotJoints GetAnyJoints() {
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                if (actionPoint.Data.RobotJoints.Count > 0)
                    return actionPoint.Data.RobotJoints[0];
            }
            throw new KeyNotFoundException("No joints available");
        }

        /// <summary>
        /// Returns orientation with id or throws KeyNotFoundException
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IO.Swagger.Model.NamedOrientation GetNamedOrientation(string id) {
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                try {
                    return actionPoint.GetOrientation(id);
                } catch (KeyNotFoundException ex) { }
            }
            throw new KeyNotFoundException("Orientations with id " + id + " not found");
        }

        public IO.Swagger.Model.NamedOrientation GetAnyNamedOrientation() {
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                if (actionPoint.Data.Orientations.Count > 0)
                    return actionPoint.Data.Orientations[0];
            }
            throw new ItemNotFoundException("No orientation available");
        }

        public ActionPoint GetAnyActionPoint() {
            if (ActionPoints.Count > 0)
                return ActionPoints.First().Value;
            throw new ItemNotFoundException("No action point available");
        }

        /// <summary>
        /// Returns action point containing orientation with id or throws KeyNotFoundException
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionPoint GetActionPointWithOrientation(string id) {
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                try {
                    // if GetOrientation dont throw exception, correct action point was found
                    actionPoint.GetOrientation(id);
                    return actionPoint;
                } catch (KeyNotFoundException) { }
            }
            throw new KeyNotFoundException("Action point with orientation id " + id + " not found");
        }

        /// <summary>
        /// Returns action point containing joints with id or throws KeyNotFoundException
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionPoint GetActionPointWithJoints(string id) {
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                try {
                    // if GetJoints dont throw exception, correct action point was found
                    actionPoint.GetJoints(id);
                    return actionPoint;
                } catch (KeyNotFoundException ex) { }
            }
            throw new KeyNotFoundException("Action point with joints id " + id + " not found");
        }

        /// <summary>
        /// Change size of all action points
        /// </summary>
        /// <param name="size"><0; 1> From barely visible to quite big</param>
        public void SetAPSize(float size) {
            if (ProjectMeta != null)
                PlayerPrefsHelper.SaveFloat("project/" + ProjectMeta.Id + "/APSize", size);
            APSize = size;
            foreach (ActionPoint actionPoint in GetAllActionPoints()) {
                actionPoint.SetSize(size);
            }
        }

        /// <summary>
        /// Disables all action points
        /// </summary>
        public void EnableAllActionPoints(bool enable) {
            if (!Valid)
                return;
            foreach (ActionPoint ap in ActionPoints.Values) {
                //ap.Enable(enable);
            }
        }

        /// <summary>
        /// Called when project saved. Invokes OnProjectSaved event
        /// </summary>
        internal void ProjectSaved() {
            ProjectChanged = false;
            Notifications.Instance.ShowToastMessage("Project saved successfully.");
            OnProjectSaved?.Invoke(this, EventArgs.Empty);
        }
    }
}
