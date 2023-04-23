using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using IO.Swagger.Model;
//using UnityEngine.XR.ARFoundation;
using UnityEngine.Events;
using System.Collections;
using Newtonsoft.Json;
//using MiniJSON;

namespace Base {
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
        /// Invoked when in SceneEditor or ProjectEditor state and no menus are opened
        /// </summary>
        public event EventHandler OnSceneInteractable;
        /// <summary>
        /// Invoked when any menu is opened
        /// </summary>
        public event EventHandler OnSceneNotInteractable; 
        /// <summary>
        /// Invoked when game state changed. Contains new state
        /// </summary>
        public event AREditorEventArgs.GameStateEventHandler OnGameStateChanged;
        /// <summary>
        /// Invoked when editor state changed. Contains new state
        /// </summary>
        public event AREditorEventArgs.EditorStateEventHandler OnEditorStateChanged;
        /// <summary>
        /// Invoked when project editor is opened
        /// </summary>
        public event EventHandler OnOpenProjectEditor;
        /// <summary>
        /// Invoked when scene editor is opened
        /// </summary>
        public event EventHandler OnOpenSceneEditor;
        /// <summary>
        /// Invoked when main screen is opened
        /// </summary>
        public event EventHandler OnOpenMainScreen;
        /// <summary>
        /// Invoked upon action execution. Contains ID of executed action
        /// </summary>
        public event AREditorEventArgs.StringEventHandler OnActionExecution;
        /// <summary>
        /// Invoked when action execution finished
        /// </summary>
        public event EventHandler OnActionExecutionFinished;
        /// <summary>
        /// Invoked when action execution was canceled
        /// </summary>
        public event EventHandler OnActionExecutionCanceled;

        /// <summary>
        /// Holds current application state (opened screen)
        /// </summary>
        private GameStateEnum gameState;
        /// <summary>
        /// Holds current editor state
        /// </summary>
        private EditorStateEnum editorState;
        /// <summary>
        /// Prefab for transform gizmo
        /// </summary>
        public GameObject GizmoPrefab;
        /// <summary>
        /// Loading screen with animation
        /// </summary>
        //public LoadingScreen LoadingScreen;
        /// <summary>
        /// Canvas group of main menu button (hamburger menu in editor screen)
        /// </summary>
        public CanvasGroup MainMenuBtnCG;
        /// <summary>
        /// Standard button prefab
        /// </summary>
        public GameObject ButtonPrefab;
        /// <summary>
        /// Service button prefab - with green or red strip on the left side (joints buttons)
        /// </summary>
        public GameObject ServiceButtonPrefab;
        /// <summary>
        /// Tooltip gameobject
        /// </summary>
        public GameObject Tooltip;
        /// <summary>
        /// Gameobject for floating point number input (with label)
        /// </summary>
        public GameObject LabeledFloatInput;
        /// <summary>
        /// Text component of tooltip
        /// </summary>
        public TMPro.TextMeshProUGUI Text;
        /// <summary>
        /// Temp storage for delayed project
        /// </summary>
        private IO.Swagger.Model.Project newProject;
        /// <summary>
        /// Temp storage for delayed scene
        /// </summary>
        private IO.Swagger.Model.Scene newScene;
        /// <summary>
        /// Temp storage for delayed package
        /// </summary>
        private PackageStateData newPackageState, nextPackageState;

        /// <summary>
        /// Indicates that project should be opened with delay (waiting for scene or action objects)
        /// </summary>
        private bool openProject = false;
        /// <summary>
        /// Indicates that scene should be opened with delay (waiting for action objects)
        /// </summary>
        private bool openScene = false;
        /// <summary>
        /// Indicates that package should be opened with delay (waiting for scene or action objects)
        /// </summary>
        private bool openPackage = false;
        /// <summary>
        /// Id of action which runs when initializing package
        /// </summary>
        public string ActionRunningOnStartupId;

        /// <summary>
        /// Holds ID of currently executing action. Null if there is no such action
        /// </summary>
        public string ExecutingAction = null;
        /// <summary>
        /// Api version
        /// </summary>        
        public const string ApiVersion = "1.0.0";
        /// <summary>
        /// List of projects metadata
        /// </summary>
        public List<IO.Swagger.Model.ListProjectsResponseData> Projects = new List<IO.Swagger.Model.ListProjectsResponseData>();
        /// <summary>
        /// List of packages metadata
        /// </summary>
        public List<IO.Swagger.Model.PackageSummary> Packages = new List<IO.Swagger.Model.PackageSummary>();
        /// <summary>
        /// List of scenes metadata
        /// </summary>
        public List<IO.Swagger.Model.ListScenesResponseData> Scenes = new List<IO.Swagger.Model.ListScenesResponseData>();
        /// <summary>
        /// 
        /// </summary>
        public TMPro.TMP_Text MessageBox;
        /// <summary>
        /// Connection info component in main menu
        /// </summary>
        public TMPro.TMP_Text ConnectionInfo;
        /// <summary>
        /// Server version info component in main menu
        /// </summary>
        public TMPro.TMP_Text ServerVersion;

        /// <summary>
        /// GameObject which is currently manipulated by gizmo
        /// </summary>
        public GameObject ObjectWithGizmo;

        /// <summary>
        /// Canvas for headUp info (notifications, tooltip, loading screen etc.
        /// </summary>
        [SerializeField]
        private Canvas headUpCanvas;

        /// <summary>
        /// Holds info about server (version, supported RPCs, supported parameters etc.)
        /// </summary>
        public IO.Swagger.Model.SystemInfoResponseData SystemInfo;
        /// <summary>
        /// Holds info about currently running package
        /// </summary>
        public PackageInfoData PackageInfo;

        /// <summary>
        /// Holds whether delayed openning of main screen is requested
        /// </summary>
        private bool openMainScreenRequest = false;

        /// <summary>
        /// Holds info about what part of main screen should be displayd
        /// </summary>
        private ShowMainScreenData openMainScreenData;

        /// <summary>
        /// Holds info abour AR session
        /// </summary>
        [SerializeField]
        //private ARSession ARSession;

        /// <summary>
        /// Callback to be invoked when requested object is selected and potentionally validated
        /// </summary>
        private Action<object> ObjectCallback;

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
        /// Enum specifying editor states
        ///
        /// For selecting states - other interaction than selecting of requeste object is disabled
        /// </summary>
        public enum EditorStateEnum {
            /// <summary>
            /// No editor (scene or project) opened
            /// </summary>
            Closed,
            /// <summary>
            /// Normal state
            /// </summary>
            Normal,
            /// <summary>
            /// Indicates that user should select action object
            /// </summary>
            SelectingActionObject,
            /// <summary>
            /// Indicates that user should select action point
            /// </summary>
            SelectingActionPoint,
            /// <summary>
            /// Indicates that user should select action 
            /// </summary>
            SelectingAction,
            /// <summary>
            /// Indicates that user should select action input
            /// </summary>
            SelectingActionInput,
            /// <summary>
            /// Indicates that user should select action output
            /// </summary>
            SelectingActionOutput,
            /// <summary>
            /// Indicates that user should select action object or another action point
            /// </summary>
            SelectingActionPointParent,
            /// <summary>
            /// Indicates that user should select orientation of action point
            /// </summary>
            SelectingAPOrientation,
            /// <summary>
            /// Indicates that user should select end effector
            /// </summary>
            SelectingEndEffector,
            /// <summary>
            /// Indicates that all interaction is disabled
            /// </summary>
            InteractionDisabled
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
            // request for delayed openning of main screen to allow loading of action objects and their actions
            if (openMainScreenRequest && ActionsManager.Instance.ActionsReady) {
                openMainScreenRequest = false;
                //await OpenMainScreen(openMainScreenData.What, openMainScreenData.Highlight);
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
        /// Change editor state and enable / disable UI elements based on the new state
        /// and invoke corresponding event
        /// </summary>
        /// <param name="newState">New state</param>
        public void SetEditorState(EditorStateEnum newState) {
            editorState = newState;
            OnEditorStateChanged?.Invoke(this, new EditorStateEventArgs(newState));
            switch (newState) {
                // when normal state, enable main menu button and status panel
                case EditorStateEnum.Normal:
                    //EditorHelper.EnableCanvasGroup(MainMenuBtnCG, true);
                    break;
                // otherwise, disable main menu button and status panel
                default:
                    //EditorHelper.EnableCanvasGroup(MainMenuBtnCG, false);
                    break;
            }
        }

        /// <summary>
        /// Returns editor state
        /// </summary>
        /// <returns>Editor state</returns>
        public EditorStateEnum GetEditorState() {
            return editorState;
        }

        /// <summary>
        /// Switch editor to one of selecting modes (based on request type) and promts user
        /// to select object / AP / etc. 
        /// </summary>
        /// <param name="requestType">Determines what the user should select</param>
        /// <param name="callback">Action which is called when object is selected and (optionaly) validated</param>
        /// <param name="message">Message displayed to the user</param>
        /// <param name="validationCallback">Action to be called when user selects object. If returns true, callback is called,
        /// otherwise waits for selection of another object</param>
        //public async Task RequestObject(EditorStateEnum requestType, Action<object> callback, string message, Func<object, Task<RequestResult>> validationCallback = null, UnityAction onCancelCallback = null) {
        //    // only for "selection" requests
        //    Debug.Assert(requestType != EditorStateEnum.Closed &&
        //        requestType != EditorStateEnum.Normal &&
        //        requestType != EditorStateEnum.InteractionDisabled);
        //    SetEditorState(requestType);

        //    SelectorMenu.Instance.PointsToggle.SetInteractivity(false);
        //    SelectorMenu.Instance.ActionsToggle.SetInteractivity(false);
        //    SelectorMenu.Instance.IOToggle.SetInteractivity(false);
        //    SelectorMenu.Instance.ObjectsToggle.SetInteractivity(false);
        //    SelectorMenu.Instance.OthersToggle.SetInteractivity(false);
        //    SelectorMenu.Instance.RobotsToggle.SetInteractivity(false);

        //    // "disable" non-relevant elements to simplify process for the user
        //    switch (requestType) {
        //        case EditorStateEnum.SelectingActionObject:
        //            SelectorMenu.Instance.RobotsToggle.SetInteractivity(true);
        //            SelectorMenu.Instance.ObjectsToggle.SetInteractivity(true);
        //            SceneManager.Instance.EnableAllActionObjects(true, true);
        //            ProjectManager.Instance.EnableAllActionPoints(false);
        //            ProjectManager.Instance.EnableAllActions(false);
        //            ProjectManager.Instance.EnableAllOrientations(false);
        //            if (SceneManager.Instance.SceneStarted)
        //                await ProjectManager.Instance.EnableAllRobotsEE(false);
        //            break;
        //        case EditorStateEnum.SelectingActionOutput:
        //            ProjectManager.Instance.EnableAllActionPoints(true);
        //            ProjectManager.Instance.EnableAllActions(true);
        //            SceneManager.Instance.EnableAllActionObjects(false);
        //            ProjectManager.Instance.EnableAllOrientations(false);
        //            if (SceneManager.Instance.SceneStarted)
        //                await ProjectManager.Instance.EnableAllRobotsEE(false);
        //            break;
        //        case EditorStateEnum.SelectingActionInput:
        //            ProjectManager.Instance.EnableAllActionPoints(true);
        //            ProjectManager.Instance.EnableAllActions(true);
        //            SceneManager.Instance.EnableAllActionObjects(false);
        //            ProjectManager.Instance.EnableAllOrientations(false);
        //            if (SceneManager.Instance.SceneStarted)
        //                await ProjectManager.Instance.EnableAllRobotsEE(false);
        //            break;
        //        case EditorStateEnum.SelectingActionPointParent:
        //            SelectorMenu.Instance.RobotsToggle.SetInteractivity(true);
        //            SelectorMenu.Instance.ObjectsToggle.SetInteractivity(true);
        //            SelectorMenu.Instance.PointsToggle.SetInteractivity(true);
        //            ProjectManager.Instance.EnableAllActions(false);
        //            ProjectManager.Instance.EnableAllOrientations(false);
        //            if (SceneManager.Instance.SceneStarted)
        //                await ProjectManager.Instance.EnableAllRobotsEE(false);
        //            SceneManager.Instance.EnableAllActionObjects(true, true);
        //            ProjectManager.Instance.EnableAllActionPoints(true);
        //            break;
        //        case EditorStateEnum.SelectingAPOrientation:
        //            ProjectManager.Instance.EnableAllActions(false);
        //            if (SceneManager.Instance.SceneStarted)
        //                await ProjectManager.Instance.EnableAllRobotsEE(false);
        //            SceneManager.Instance.EnableAllActionObjects(true, true);
        //            ProjectManager.Instance.EnableAllActionPoints(true);
        //            ProjectManager.Instance.EnableAllOrientations(true);
        //            break;
        //        case EditorStateEnum.SelectingEndEffector:
        //            ProjectManager.Instance.EnableAllActions(false);
        //            if (SceneManager.Instance.SceneStarted)
        //                await ProjectManager.Instance.EnableAllRobotsEE(true);
        //            SceneManager.Instance.EnableAllActionObjects(false, false);
        //            SceneManager.Instance.EnableAllRobots(true);
        //            ProjectManager.Instance.EnableAllActionPoints(false);
        //            ProjectManager.Instance.EnableAllOrientations(false);
        //            break;
        //    }
        //    ObjectCallback = callback;
        //    ObjectValidationCallback = validationCallback;
        //    // display info for user and bind cancel callback,


        //    if (onCancelCallback == null) {
        //        SelectObjectInfo.Show(message, () => CancelSelection());
        //    } else {

        //        SelectObjectInfo.Show(message,
        //            () => {
        //                onCancelCallback();
        //                CancelSelection();
        //            });
        //    }
        //}


        /// <summary>
        /// Enables all visual elements (objects, actions etc.)
        /// </summary>
        //private void RestoreFilters() {
        //    SelectorMenu.Instance.UpdateFilters();
        //}

        /// <summary>
        /// Sets framerate to default value (30fps)
        /// </summary>
        public void SetDefaultFramerate() {
            Application.targetFrameRate = 30;
        }

        /// <summary>
        /// Sets framerate to higher value (120fps) for demanding operations
        /// </summary>
        public void SetTurboFramerate() {
            Application.targetFrameRate = 120;
        }

        /// <summary>
        /// Sets initial state of app
        /// </summary>
        private void Awake() {
            ConnectionStatus = ConnectionStatusEnum.Disconnected;
            OpenDisconnectedScreen();
        }

        /// <summary>
        /// Binds events and sets initial state of app
        /// </summary>
        private void Start() {
            SetDefaultFramerate();
            if (Application.isEditor || Debug.isDebugBuild) {
                //TrilleonAutomation.AutomationMaster.Initialize();
            }
            //ActionsManager.Instance.OnActionsLoaded += OnActionsLoaded;
            WebsocketManager.Instance.OnConnectedEvent += OnConnected;
            WebsocketManager.Instance.OnDisconnectEvent += OnDisconnected;
            WebsocketManager.Instance.OnShowMainScreen += OnShowMainScreen;
            WebsocketManager.Instance.OnProjectRemoved += OnProjectRemoved;
            WebsocketManager.Instance.OnProjectBaseUpdated += OnProjectBaseUpdated;
            WebsocketManager.Instance.OnSceneRemoved += OnSceneRemoved;
            WebsocketManager.Instance.OnSceneBaseUpdated += OnSceneBaseUpdated;
        }

        /// <summary>
        /// Waits until websocket is null and calls callback method (because after application pause disconnecting isn't finished completely)
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator WaitUntilWebsocketFullyDisconnected(UnityAction callback) {
            yield return new WaitWhile(() => !WebsocketManager.Instance.IsWebsocketNull());
            callback();
        }

        private void OnSceneBaseUpdated(object sender, BareSceneEventArgs args) {
            foreach (ListScenesResponseData s in Scenes) {
                if (s.Id == args.Scene.Id) {
                    s.Name = args.Scene.Name;
                    s.Modified = args.Scene.Modified;
                    break;
                }
            }
        }

        private void OnSceneRemoved(object sender, StringEventArgs args) {
            int i = 0;
            foreach (ListScenesResponseData s in Scenes) {
                if (s.Id == args.Data) {
                    Scenes.RemoveAt(i);
                    break;
                }
                i++;
            }
        }

        private void OnProjectBaseUpdated(object sender, BareProjectEventArgs args) {
            foreach (ListProjectsResponseData p in Projects) {
                if (p.Id == args.Project.Id) {
                    p.Name = args.Project.Name;
                    p.Modified = args.Project.Modified;
                    break;
                }
            }            
        }

        /// <summary>
        /// Invoked when project removed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args">ID of removed object</param>
        private void OnProjectRemoved(object sender, StringEventArgs args) {
            int i = 0;
            foreach (ListProjectsResponseData p in Projects) {
                if (p.Id == args.Data) {
                    Projects.RemoveAt(i);
                    break;
                }
                i++;
            }
        }

        /// <summary>
        /// Event called when request to open main screen come from server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void OnShowMainScreen(object sender, ShowMainScreenEventArgs args) {
            if (ActionsManager.Instance.ActionsReady)
            {
                //await OpenMainScreen(args.Data.What, args.Data.Highlight);
            }
            else {
                openMainScreenRequest = true;
                openMainScreenData = args.Data;
            }
        }

        /// <summary>
        /// Event called when disconnected from server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnDisconnected(object sender, EventArgs e) {
            
        }

        /// <summary>
        /// Event called when connected to server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnConnected(object sender, EventArgs args) {
            // initialize when connected to the server
            ExecutingAction = null;
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
                    if (!CheckApiVersion(systemInfo)) {
                        DisconnectFromSever();
                        return;
                    }
                    SystemInfo = systemInfo;

                    OnConnectedToServer?.Invoke(this, new StringEventArgs(WebsocketManager.Instance.APIDomainWS));

                    await UpdateActionObjects();

                    connectionStatus = newState;
                    break;
                case ConnectionStatusEnum.Disconnected:
                    connectionStatus = ConnectionStatusEnum.Disconnected;
                    OpenDisconnectedScreen();
                    OnDisconnectedFromServer?.Invoke(this, EventArgs.Empty);
                    Projects = new List<IO.Swagger.Model.ListProjectsResponseData>();
                    Scenes = new List<IO.Swagger.Model.ListScenesResponseData>();

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
        /// When actions are loaded, enables all menus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnActionsLoaded(object sender, EventArgs e) {
            //MainMenu.Instance.gameObject.SetActive(true);
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
                GameManager.Instance.DisconnectFromSever();
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
                SetEditorState(EditorStateEnum.InteractionDisabled);
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
        /// Get scene id based on its name
        /// </summary>
        /// <param name="name">Name of scene</param>
        /// <returns>Scene ID</returns>
        public string GetSceneId(string name) {
            foreach (ListScenesResponseData scene in Scenes) {
                if (name == scene.Name)
                    return scene.Id;
            }
            throw new RequestFailedException("No scene with name: " + name);
        }

        /// <summary>
        /// Get project id based on its name
        /// </summary>
        /// <param name="name">Name of project</param>
        /// <returns>Project ID</returns>
        public string GetProjectId(string name) {
            foreach (ListProjectsResponseData project in Projects) {
                if (name == project.Name)
                    return project.Id;
            }
            throw new RequestFailedException("No project with name: " + name);
        }

        /// <summary>
        /// Will quit the app
        /// </summary>
        public void ExitApp() => Application.Quit();

        public async void UpdateActionPointPositionUsingRobot(string actionPointId, string robotId, string arm_id, string endEffectorId) {

            try {
                await WebsocketManager.Instance.UpdateActionPointUsingRobot(actionPointId, robotId, endEffectorId, arm_id);
            } catch (RequestFailedException ex) {
                Notifications.Instance.ShowNotification("Failed to update action point", ex.Message);
            }
        }

        /// <summary>
        /// Parses version string and returns major version
        /// </summary>
        /// <param name="versionString">Version string in format 0.0.0 (major, minor, patch)</param>
        /// <returns>First number (major version)</returns>
        public int GetMajorVersion(string versionString) {
            var x = int.Parse(SplitVersionString(versionString)[0]);
            return x;
        }

        /// <summary>
        /// Parses version string and returns minor version
        /// </summary>
        /// <param name="versionString">Version string in format 0.0.0 (major, minor, patch)</param>
        /// <returns>Second number (minor version)</returns>
        public int GetMinorVersion(string versionString) {
            return int.Parse(SplitVersionString(versionString)[1]);
        }

        /// <summary>
        /// Parses version string and returns patch version
        /// </summary>
        /// <param name="versionString">Version string in format 0.0.0 (major, minor, patch)</param>
        /// <returns>Last number (patch version)</returns>
        public int GetPatchVersion(string versionString) {
            return int.Parse(SplitVersionString(versionString)[2]);
        }

        /// <summary>
        /// Splits version string and returns list of components
        /// </summary>
        /// <param name="versionString">Version string in format 0.0.0 (major.minor.patch)</param>
        /// <returns>List of components of the version string</returns>
        public List<string> SplitVersionString(string versionString) {
            List<string> version = versionString.Split('.').ToList<string>();
            Debug.Assert(version.Count == 3, versionString);
            return version;
        }

        /// <summary>
        /// Checks if api version of the connected server is compatibile with editor
        /// </summary>
        /// <param name="systemInfo">Version string in format 0.0.0 (major.minor.patch)</param>
        /// <returns>True if versions are compatibile</returns>
        public bool CheckApiVersion(IO.Swagger.Model.SystemInfoResponseData systemInfo) {
            
            if (systemInfo.ApiVersion == ApiVersion)
                return true;

            if (GetMajorVersion(systemInfo.ApiVersion) != GetMajorVersion(ApiVersion) ||
                (GetMajorVersion(systemInfo.ApiVersion) == 0 && (GetMinorVersion(systemInfo.ApiVersion) != GetMinorVersion(ApiVersion)))) {
                Notifications.Instance.ShowNotification("Incompatibile api versions", "Editor API version: " + ApiVersion + ", server API version: " + systemInfo.ApiVersion);
                return false;
            }
            Notifications.Instance.ShowNotification("Different api versions", "Editor API version: " + ApiVersion + ", server API version: " + systemInfo.ApiVersion + ". It can cause problems, you have been warned.");

            return true;
        }

        /// <summary>
        /// Opens scene editor
        /// </summary>
        public void OpenSceneEditor() {
            //Scene.SetActive(true);
            //AREditorResources.Instance.LeftMenuScene.DeactivateAllSubmenus();
            //MainMenu.Instance.Close();
            SetGameState(GameStateEnum.SceneEditor);
            OnOpenSceneEditor?.Invoke(this, EventArgs.Empty);
            SetEditorState(EditorStateEnum.Normal);
            //HideLoadingScreen(true);
        }

        /// <summary>
        /// Opens project editor
        /// </summary>
        public void OpenProjectEditor() {
#if (UNITY_ANDROID || UNITY_IOS) && AR_ON
            ARSession.enabled = true;
            if (CalibrationManager.Instance.Calibrated) {
                //Scene.SetActive(true);
            }
#else
            //Scene.SetActive(true);
#endif
            //AREditorResources.Instance.LeftMenuProject.DeactivateAllSubmenus();
            //MainMenu.Instance.Close();
            SetGameState(GameStateEnum.ProjectEditor);
            OnOpenProjectEditor?.Invoke(this, EventArgs.Empty);
            SetEditorState(EditorStateEnum.Normal);
            //HideLoadingScreen(true);
        }

        /// <summary>
        /// Waits until package is loaded
        /// </summary>
        /// <param name="timeout">TimeoutException is thrown after timeout ms when package is not loaded</param>
        public void WaitUntilPackageReady(int timeout) {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            while (PackageInfo == null) {
                if (sw.ElapsedMilliseconds > timeout)
                    throw new TimeoutException();
                System.Threading.Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Opens disconnected screen
        /// </summary>
        public void OpenDisconnectedScreen() {

            //Scene.SetActive(false);
            //SetGameState(GameStateEnum.Disconnected);
        }

        /// <summary>
        /// Gets name of project based on its ID
        /// </summary>
        /// <param name="projectId">ID of project</param>
        /// <returns>Name of project</returns>
        public string GetProjectName(string projectId) {
            foreach (ListProjectsResponseData project in Projects) {
                if (project.Id == projectId)
                    return project.Name;
            }
            throw new ItemNotFoundException("Project with id: " + projectId + " not found");
        }

        /// <summary>
        /// Gets name of scene based on its ID
        /// </summary>
        /// <param name="sceneId">ID of scene</param>
        /// <returns>Name of scene</returns>
        public string GetSceneName(string sceneId) {
            foreach (ListScenesResponseData scene in Scenes) {
                if (scene.Id == sceneId)
                    return scene.Name;
            }
            throw new ItemNotFoundException("Scene with id: " + sceneId + " not found");
        }
    }

    ///// <summary>
    ///// Universal struct for getting result of requests. 
    ///// </summary>
    //public struct RequestResult {
    //    /// <summary>
    //    /// Whether the request was successfull or not
    //    /// </summary>
    //    public bool Success;
    //    /// <summary>
    //    /// Empty when success is true, otherwise contains error description
    //    /// </summary>
    //    public string Message;

    //    public RequestResult(bool success, string message) {
    //        this.Success = success;
    //        this.Message = message;
    //    }

    //    public RequestResult(bool success) {
    //        this.Success = success;
    //        this.Message = "";
    //    }

    //    public override bool Equals(object obj) {
    //        return obj is RequestResult other &&
    //               Success == other.Success &&
    //               Message == other.Message;
    //    }

    //    public override int GetHashCode() {
    //        int hashCode = 151515764;
    //        hashCode = hashCode * -1521134295 + Success.GetHashCode();
    //        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Message);
    //        return hashCode;
    //    }

    //    public void Deconstruct(out bool success, out string message) {
    //        success = Success;
    //        message = Message;
    //    }

    //    public static implicit operator (bool success, string message)(RequestResult value) {
    //        return (value.Success, value.Message);
    //    }

    //    public static implicit operator RequestResult((bool success, string message) value) {
    //        return new RequestResult(value.success, value.message);
    //    }
    //}
}
