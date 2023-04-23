using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using IO.Swagger.Model;

namespace Base
{
    /// <summary>
    /// Main controller of application. It is responsible for management of different screens
    /// (landing screen, main screen, editor screens) and for management of application states.
    /// </summary>
    public class GameManager : Singleton<GameManager> {
        #region fields
        /// <summary>
        /// Temp storage for delayed project
        /// </summary>
        private IO.Swagger.Model.Project newProject;
        /// <summary>
        /// Temp storage for delayed scene
        /// </summary>
        private IO.Swagger.Model.Scene newScene;
        /// <summary>
        /// Indicates that project should be opened with delay (waiting for scene or action objects)
        /// </summary>
        private bool openProject = false;
        /// <summary>
        /// Indicates that scene should be opened with delay (waiting for action objects)
        /// </summary>
        private bool openScene = false;

        #endregion

        /// <summary>
        /// Enum specifying connection states
        /// </summary>
        public enum ConnectionStatusEnum {
            Connected, Disconnected, Connecting
        }
       
        /// <summary>
        /// Holds info of connection status
        /// </summary>
        private ConnectionStatusEnum connectionStatus;

        /// <summary>
        /// When connected to server, checks for requests for delayd scene, project, package or main screen openning
        /// </summary>
        private void Update() {
            // Only when connected to server
            if (ConnectionStatus != ConnectionStatusEnum.Connected)
                return;
            // request for delayed openning of scene to allow loading of action objects and their actions
            if (openScene) {
                openScene = false;
                if (newScene != null) {
                    Scene scene = newScene;
                    newScene = null;
                    SceneOpened(scene);
                }
                // request for delayed openning of project to allow loading of action objects and their actions
            } else if (openProject) {
                openProject = false;
                if (newProject != null && newScene != null) {
                    Scene scene = newScene;
                    Project project = newProject;
                    newScene = null;
                    newProject = null;
                    ProjectOpened(scene, project);
                }
                // request for delayed openning of package to allow loading of action objects and their actions
            } 
        }

        /// <summary>
        /// Holds connection status and invokes callback when status changed
        /// </summary>
        public ConnectionStatusEnum ConnectionStatus {
            get => connectionStatus; 
            set {
                if (connectionStatus != value) {
                    OnConnectionStatusChanged(value);
                }
            }
        }

        /// <summary>
        /// Sets initial state of app
        /// </summary>
        private void Awake() {
            ConnectionStatus = ConnectionStatusEnum.Disconnected;
        }

        /// <summary>
        /// Binds events and sets initial state of app
        /// </summary>
        private void Start() {
            WebsocketManager.Instance.OnConnectedEvent += OnConnected;
        }

        /// <summary>
        /// Event called when connected to server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnConnected(object sender, EventArgs args) {
            ConnectionStatus = ConnectionStatusEnum.Connected;
        }

        /// <summary>
        /// Event called when connections status chanched
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void OnConnectionStatusChanged(ConnectionStatusEnum newState) {
            switch (newState) {
                case ConnectionStatusEnum.Connected:
                    try {
                        await WebsocketManager.Instance.RegisterUser("ARProjection");
                    } catch (RequestFailedException ex) {
                        DisconnectFromSever();
                        Notifications.Instance.ShowNotification("Connection failed", ex.Message);
                        return;
                    }
                    await UpdateActionObjects();

                    connectionStatus = newState;
                    break;
                case ConnectionStatusEnum.Disconnected:
                    connectionStatus = ConnectionStatusEnum.Disconnected;
                    ProjectManager.Instance.DestroyProject();
                    SceneManager.Instance.DestroyScene();
                    break;
            }
        }

        /// <summary>
        /// Connects to server
        /// </summary>
        /// <param name="domain">hostname or IP address</param>
        /// <param name="port">Port of ARServer</param>
        public void ConnectToSever(string domain, int port) {
            WebsocketManager.Instance.ConnectToServer(domain, port);
        }

        /// <summary>
        /// Disconnects from server
        /// </summary>
        public void DisconnectFromSever() {
            ConnectionStatus = ConnectionStatusEnum.Disconnected;
            WebsocketManager.Instance.DisconnectFromSever();
        }

        /// <summary>
        /// Updates action objects and their actions from server
        /// </summary>
        /// <param name="highlightedObject">When set, object with this ID will gets highlighted for a few seconds in menu
        /// to inform user about it</param>
        /// <returns></returns>
        public async Task UpdateActionObjects() {
            try {
                List<IO.Swagger.Model.ObjectTypeMeta> objectTypeMetas = await WebsocketManager.Instance.GetObjectTypes();
                ActionsManager.Instance.UpdateObjects(objectTypeMetas);
            } catch (RequestFailedException ex) {
                Debug.LogError(ex);
                Instance.DisconnectFromSever();
            }
        }
      
        /// <summary>
        /// Create visual elements of opened scene and open scene editor
        /// </summary>
        /// <param name="scene">Scene desription from the server</param>
        /// <returns></returns>
        internal void SceneOpened(Scene scene) {
            if (!ActionsManager.Instance.ActionsReady) {
                newScene = scene;
                openScene = true;
                return;
            }
            try {
                if (!SceneManager.Instance.CreateScene(scene)) {
                    Notifications.Instance.SaveLogs(scene, null, "Failed to initialize scene");
                }
            } catch (TimeoutException ex) {
                Debug.LogError(ex);
                Notifications.Instance.SaveLogs(scene, null, "Failed to initialize scene");
            } 
        }

        /// <summary>
        /// Create visual elements of opened scene and project and open project editor
        /// </summary>
        /// <param name="project">Project desription from the server</param>
        /// <returns></returns>
        internal void ProjectOpened(Scene scene, Project project) {
            if (!ActionsManager.Instance.ActionsReady) {
                newProject = project;
                newScene = scene;
                openProject = true;
                return;
            }
            try {
                if (!SceneManager.Instance.CreateScene(scene)) {
                    Notifications.Instance.SaveLogs(scene, project, "Failed to initialize scene");
                    return;
                }
                if (!ProjectManager.Instance.CreateProject(project, true)) {
                    Notifications.Instance.SaveLogs(scene, project, "Failed to initialize project");
                    //HideLoadingScreen();
                }
            } catch (TimeoutException ex) {
                Debug.LogError(ex);
                Notifications.Instance.SaveLogs(scene, project, "Failed to initialize project");
            }
        }

        /// <summary>
        /// Callback when scene was closed
        /// </summary>
        internal void SceneClosed() {
            SceneManager.Instance.DestroyScene();
        }

        /// <summary>
        /// Callback when project was closed
        /// </summary>
        internal void ProjectClosed() {
            ProjectManager.Instance.DestroyProject();
            SceneManager.Instance.DestroyScene();
        }
    }
}
