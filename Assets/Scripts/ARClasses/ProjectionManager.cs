//using UnityEngine.XR.ARFoundation;
using Base;
using System.Threading.Tasks;
using UnityEngine;
//using MiniJSON;


namespace Assets.Scripts.ARClasses
{
    /// <summary>
    /// Main controller of projection.
    /// </summary>
    public class ProjectionManager : Singleton<ProjectionManager>
    {
        /// <summary>
        /// Calibration
        /// </summary>
        public CalibrationData calibrationData;
        public string calibXmlPath = "calibration_result.xml";
        public GameObject kinect;
        public GameObject projector;
        public GameObject canvas;
        public GameObject canvasScene;
        public GameObject actionPointPrefab;
        public Camera mainCamera;


        /// <summary>
        /// Prefab for Projector
        /// </summary>        
        public GameObject ProjectorPrefab;

        private void Start()
        {
            calibrationData = new CalibrationData(calibXmlPath);
            if (Camera.main)
            {
                mainCamera = Camera.main;
            }
        }

        public async Task SetupProjection(GameObject kinectObj, IO.Swagger.Model.SceneObject sceneObject)
        {
            if (mainCamera)
            {
                mainCamera.enabled = false;
            }
            kinect = kinectObj;
            calibrationData.KinectPosition = kinectObj.transform;

            await calibrationData.GetCameraParameters(sceneObject.Id);

            projector = Instantiate(ProjectorPrefab, SceneManager.Instance.ActionObjectsSpawn.transform);
            KinectCoordConversion.SetProjectorTransform(ProjectionManager.Instance.projector);
            projector.name = "Projector";
        }

        public void DestroyProjection()
        {
            if (mainCamera)
            {
                mainCamera.enabled = true;
            }
            Destroy(projector);
            foreach (Transform child in canvasScene.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
