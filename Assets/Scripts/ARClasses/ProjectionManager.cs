using Base;
using UnityEngine;
using UnityEngine.U2D;

namespace Assets.Scripts.ARClasses
{
    /// <summary>
    /// Main controller of projection.
    /// </summary>
    public class ProjectionManager : Singleton<ProjectionManager>
    {
        public CalibrationData calibrationData;
        private string calibXmlPath = "Calibration\\calibration_result.xml";

        /// <summary>
        /// /references to game objects
        /// </summary>
        public GameObject kinect;
        public GameObject projector;
        public Camera mainCamera;

        /// <summary>
        /// scale of projected objects
        /// </summary>
        public float scaleModifier = 2.5f;

        /// <summary>
        /// Projector Prefab to be created
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
            //UpdateProjectorTransform();
            projector.name = "Projector";

            PixelPerfectCamera cam = projector.GetComponent<PixelPerfectCamera>();
            cam.refResolutionX = calibrationData.Width;
            cam.refResolutionY = calibrationData.Height;
            SceneManager.Instance.CanvasScene.SetActive(true);
        }

        /// <summary>
        /// Set projector transform based on calibration data.
        /// </summary>
        public void UpdateProjectorTransform()
        {
            //get actual kinect transform
            GameObject actualKinect = kinect.transform.GetChild(0).gameObject;

            projector.transform.position = actualKinect.transform.position + calibrationData.Translation;
            Matrix4x4 rotation = calibrationData.Rotation.inverse;
            Quaternion rotationQuaternion = Quaternion.LookRotation(rotation.GetColumn(2), rotation.GetColumn(1));
            projector.transform.rotation = actualKinect.transform.rotation * rotationQuaternion;
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
            SceneManager.Instance.CanvasScene.SetActive(false);
        }
    }
}
