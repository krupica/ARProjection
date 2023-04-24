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

        public void ResetAllPositions()
        {
            foreach (var ao in SceneManager.Instance.ActionObjects)
            {
                ao.Value.ResetPosition();
            }
            foreach (var ap in ProjectManager.Instance.ActionPoints)
            {
                ap.Value.ResetPosition();
            }
        }

        public void SetupProjection(GameObject kinectObj)
        {
            if (mainCamera)
            {
                mainCamera.enabled = false;
            }
            kinect = kinectObj;

            projector = Instantiate(ProjectorPrefab, SceneManager.Instance.World.transform);
            UpdateProjectorTransform();
            projector.name = "Projector";

            ResetAllPositions();
        }

        public void UpdateProjectorTransform()
        {
            projector.transform.position = kinect.transform.position + calibrationData.Translation;
            Matrix4x4 rotation = calibrationData.Rotation.inverse;
            Quaternion rotationQuaternion = Quaternion.LookRotation(rotation.GetColumn(2), rotation.GetColumn(1));
            projector.transform.rotation = kinect.transform.rotation * rotationQuaternion;
            ResetAllPositions();
        }

        public void DestroyProjection()
        {
            if (mainCamera)
            {
                mainCamera.enabled = true;
            }
            if (projector)
            {
                Destroy(projector);
            }
            foreach (Transform child in canvasScene.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
