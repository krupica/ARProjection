using System.Collections.Generic;
using UnityEngine;
using IO.Swagger.Model;
using System.Linq;
using Assets.Scripts.ARClasses;
using ActionPoint = Assets.Scripts.ARClasses.ActionPoint;

//This class was taken from AREditor and modified for the needs of this application. (https://github.com/robofit/arcor2_areditor)
namespace Base
{
    /// <summary>
    /// Takes care of currently opened project. Provides methods for manipuation with project.
    /// </summary>
    public class ProjectManager : Base.Singleton<ProjectManager> {

        #region fields
        /// <summary>
        /// All action points in scene
        /// </summary>
        public Dictionary<string, ActionPoint> ActionPoints = new Dictionary<string, ActionPoint>();
        /// <summary>
        /// Spawn point for global action points
        /// </summary>
        public GameObject ActionPointsOrigin;
        /// <summary>
        /// Prefab for project elements
        /// </summary>
        public GameObject ActionPointPrefab;
        /// <summary>
        /// Holds current diameter of action points
        /// </summary>
        public float APSize = 0.2f;
        
        #endregion

        /// <summary>
        /// Initialization of projet manager
        /// </summary>
        private void Start() {
            WebsocketManager.Instance.OnActionPointAdded += OnActionPointAdded;
            WebsocketManager.Instance.OnActionPointRemoved += OnActionPointRemoved;
            WebsocketManager.Instance.OnActionPointBaseUpdated += OnActionPointBaseUpdated;
        }

        private void OnActionPointBaseUpdated(object sender, BareActionPointEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPoint(args.ActionPoint.Id);
                actionPoint.ActionPointBaseUpdate(args.ActionPoint);
            } catch (KeyNotFoundException ex) {
                Debug.Log("Action point " + args.ActionPoint.Id + " not found!");
                Notifications.Instance.ShowNotification("", "Action point " + args.ActionPoint.Id + " not found!");
                return;
            }
        }

        private void OnActionPointRemoved(object sender, StringEventArgs args) {
            RemoveActionPoint(args.Data);
        }

        private void OnActionPointAdded(object sender, ProjectActionPointEventArgs data) {
            SpawnActionPoint(data.ActionPoint);
        }

        /// <summary>
        /// Creates project from given json
        /// </summary>
        /// <param name="project">Project descriptoin in json</param>
        /// <param name="allowEdit">Sets if project is editable</param>
        /// <returns>True if project sucessfully created</returns>
        public bool CreateProject(IO.Swagger.Model.Project project, bool allowEdit) {
            UpdateActionPoints(project);
            return true;
        }

        /// <summary>
        /// Destroys current project
        /// </summary>
        /// <returns>True if project successfully destroyed</returns>
        public bool DestroyProject() {
            foreach (ActionPoint ap in ActionPoints.Values) {
                ap.DeleteAP(false);
            }
            ActionPoints.Clear();
            ProjectionManager.Instance.DestroyProjection();
            return true;
        }

        /// <summary>
        /// Spawn action point into the project
        /// </summary>
        /// <param name="apData">Json describing action point</param>
        /// <param name="actionPointParent">Parent of action point. If null, AP is spawned as global.</param>
        /// <returns></returns>
        public ActionPoint SpawnActionPoint(IO.Swagger.Model.ActionPoint apData) {
            Debug.Assert(apData != null);
            GameObject AP = Instantiate(ActionPointPrefab, ActionPointsOrigin.transform);
            
            AP.transform.localScale = new Vector3(1f, 1f, 1f);
            ActionPoint actionPoint = AP.GetComponent<ActionPoint>();
            actionPoint.InitAP(apData);
            ActionPoints.Add(actionPoint.Data.Id, actionPoint);
            return actionPoint;
        }

        /// <summary>
        /// Updates action point GameObject in ActionObjects.ActionPoints dict based on the data present in IO.Swagger.Model.ActionPoint Data.
        /// </summary>
        /// <param name="project"></param>
        public void UpdateActionPoints(Project project) {
            List<string> currentAP = new List<string>();
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

                        //actionPoint.UpdatePositionsOfPucks();

                        currentAP.Add(actionPoint.Data.Id);

                        processedParents.Add(projectActionPoint.Id);
                    }
                }
            }

            // Remove deleted action points
            foreach (string actionPointId in ActionPoints.Keys.ToList<string>()) {
                if (!currentAP.Contains(actionPointId)) {
                    RemoveActionPoint(actionPointId);
                }
            }
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
    }
}
