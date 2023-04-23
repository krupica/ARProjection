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
        /// Called when project is closed
        /// </summary>
        public event EventHandler OnCloseProject;
        /// <summary>
        /// Called when scene is closed
        /// </summary>
        public event EventHandler OnCloseScene;
        /// <summary>
        /// Called when editor connected to server. Contains server URI
        /// </summary>
        public event AREditorEventArgs.StringEventHandler OnConnectedToServer;
        /// <summary>
        /// Called when editor is trying to connect to server. Contains server URI
        /// </summary>
        public event AREditorEventArgs.StringEventHandler OnConnectingToServer;
        /// <summary>
        /// Called when disconected from server
        /// </summary>
        public event EventHandler OnDisconnectedFromServer;
        /// <summary>
        /// Called when some element of scene changed (action object)
        /// </summary>
        public event EventHandler OnSceneChanged;
        /// <summary>
        /// Called when some action object changed
        /// </summary>
        public event EventHandler OnActionObjectsChanged;
        /// <summary>
        /// Invoked when game state changed. Contains new state
        /// </summary>
        public event AREditorEventArgs.GameStateEventHandler OnGameStateChanged;
        /// <summary>
        /// Holds current application state (opened screen)
        /// </summary>
        private GameStateEnum gameState;
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
        /// Enum specifying aplication states
        /// </summary>
        public enum GameStateEnum {
            /// <summary>
            /// Not connected to server
            /// </summary>
            Disconnected,
            /// <summary>
            /// Screen with list of scenes, projects and packages
            /// </summary>
            MainScreen,
            /// <summary>
            /// Scene editor
            /// </summary>
            SceneEditor,
            /// <summary>
            /// Project editor
            /// </summary>
            ProjectEditor,
            /// <summary>
            /// Visualisation of running package
            /// </summary>
            PackageRunning,
            LoadingScene,
            LoadingProject,
            LoadingPackage,
            ClosingScene,
            ClosingProject,
            ClosingPackage,
            None
        }

        /// <summary>
        /// Holds info of connection status
        /// </summary>
        private ConnectionStatusEnum connectionStatus;

        //NEMAZAT
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
        /// Returns current game state
        /// </summary>
        /// <returns>Current game state</returns>
        public GameStateEnum GetGameState() {
            return gameState;
        }

        /// <summary>
        /// Change game state and invoke coresponding event
        /// </summary>
        /// <param name="value">New game state</param>
        public void SetGameState(GameStateEnum value) {
            gameState = value;            
            OnGameStateChanged?.Invoke(this, new GameStateEventArgs(gameState));            
        }

        /// <summary>
        /// Sets framerate to default value (30fps)
        /// </summary>
        public void SetDefaultFramerate() {
            Application.targetFrameRate = 30;
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
            SetDefaultFramerate();
            WebsocketManager.Instance.OnConnectedEvent += OnConnected;
        }

        /// <summary>
        /// Event called when connected to server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnConnected(object sender, EventArgs args) {
            // initialize when connected to the server
            ConnectionStatus = GameManager.ConnectionStatusEnum.Connected;
        }

        /// <summary>
        /// Event called when connections status chanched
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void OnConnectionStatusChanged(ConnectionStatusEnum newState) {
            switch (newState) {
                case ConnectionStatusEnum.Connected:
                    IO.Swagger.Model.SystemInfoResponseData systemInfo;
                    try {
                        systemInfo = await WebsocketManager.Instance.GetSystemInfo();
                        await WebsocketManager.Instance.RegisterUser("ARProjection");
                    } catch (RequestFailedException ex) {
                        DisconnectFromSever();
                        Notifications.Instance.ShowNotification("Connection failed", ex.Message);
                        return;
                    }
                    OnConnectedToServer?.Invoke(this, new StringEventArgs(WebsocketManager.Instance.APIDomainWS));

                    await UpdateActionObjects();

                    connectionStatus = newState;
                    break;
                case ConnectionStatusEnum.Disconnected:
                    connectionStatus = ConnectionStatusEnum.Disconnected;
                    OnDisconnectedFromServer?.Invoke(this, EventArgs.Empty);
                    ProjectManager.Instance.DestroyProject();
                    SceneManager.Instance.DestroyScene();
                    //Scene.SetActive(false);
                    break;
            }
        }

        /// <summary>
        /// Connects to server
        /// </summary>
        /// <param name="domain">hostname or IP address</param>
        /// <param name="port">Port of ARServer</param>
        public async void ConnectToSever(string domain, int port) {
            //ShowLoadingScreen("Connecting to server");
            OnConnectingToServer?.Invoke(this, new StringEventArgs(WebsocketManager.Instance.GetWSURI(domain, port)));
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
        /// When package runs failed with exception, show notification to the user
        /// </summary>
        /// <param name="data"></param>
        internal void HandleProjectException(ProjectExceptionData data) {
            Notifications.Instance.ShowNotification("Project exception", data.Message);
        }

        /// <summary>
        /// Create visual elements of opened scene and open scene editor
        /// </summary>
        /// <param name="scene">Scene desription from the server</param>
        /// <returns></returns>
        internal void SceneOpened(Scene scene) {
            SetGameState(GameStateEnum.LoadingScene);
            if (!ActionsManager.Instance.ActionsReady) {
                newScene = scene;
                openScene = true;
                return;
            }
            try {
                if (SceneManager.Instance.CreateScene(scene)) {                    
                    OpenSceneEditor();                    
                } else {
                    Notifications.Instance.SaveLogs(scene, null, "Failed to initialize scene");
                    //HideLoadingScreen();
                }
            } catch (TimeoutException ex) {
                Debug.LogError(ex);
                Notifications.Instance.SaveLogs(scene, null, "Failed to initialize scene");
                //HideLoadingScreen();
            } 
        }

        /// <summary>
        /// Create visual elements of opened scene and project and open project editor
        /// </summary>
        /// <param name="project">Project desription from the server</param>
        /// <returns></returns>
        internal async Task ProjectOpened(Scene scene, Project project) {
            var state = GetGameState();
            if (!ActionsManager.Instance.ActionsReady) {
                newProject = project;
                newScene = scene;
                openProject = true;
                return;
            }
            if (GetGameState() == GameStateEnum.SceneEditor) {
                SceneManager.Instance.DestroyScene();
            }
            SetGameState(GameStateEnum.LoadingProject);
            try {
                if (!SceneManager.Instance.CreateScene(scene)) {
                    Notifications.Instance.SaveLogs(scene, project, "Failed to initialize scene");
                    Debug.LogError("wft");
                    //HideLoadingScreen();
                    return;
                }
                if (await ProjectManager.Instance.CreateProject(project, true)) {
                    OpenProjectEditor();
                } else {
                    Notifications.Instance.SaveLogs(scene, project, "Failed to initialize project");
                    //HideLoadingScreen();
                }
            } catch (TimeoutException ex) {
                Debug.LogError(ex);
                Notifications.Instance.SaveLogs(scene, project, "Failed to initialize project");
                //HideLoadingScreen();
            }
        }

        /// <summary>
        /// Callback when scene was closed
        /// </summary>
        internal void SceneClosed() {
            SetGameState(GameStateEnum.ClosingScene);
            //ShowLoadingScreen();
            SceneManager.Instance.DestroyScene();
            OnCloseScene?.Invoke(this, EventArgs.Empty);
            SetGameState(GameStateEnum.None);
        }

        /// <summary>
        /// Callback when project was closed
        /// </summary>
        internal void ProjectClosed() {
            SetGameState(GameStateEnum.ClosingProject);
            //ShowLoadingScreen();
            ProjectManager.Instance.DestroyProject();
            SceneManager.Instance.DestroyScene();
            OnCloseProject?.Invoke(this, EventArgs.Empty);
            SetGameState(GameStateEnum.None);
        }

        /// <summary>
        /// Will quit the app
        /// </summary>
        public void ExitApp() => Application.Quit();

        /// <summary>
        /// Opens scene editor
        /// </summary>
        public void OpenSceneEditor() {
            SetGameState(GameStateEnum.SceneEditor);
        }

        /// <summary>
        /// Opens project editor
        /// </summary>
        public void OpenProjectEditor() {
            SetGameState(GameStateEnum.ProjectEditor);
        }
    }
}
