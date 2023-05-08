//using UnityEngine.XR.ARFoundation;
using Base;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
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
        private string calibXmlPath = "Calibration\\calibration_result.xml";

        public GameObject kinect;
        public GameObject projector;
        public Camera mainCamera;
        public float scaleModifier = 2.5f;

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
            //UpdateProjectorTransform();
            projector.name = "Projector";

            PixelPerfectCamera cam = projector.GetComponent<PixelPerfectCamera>();
            cam.refResolutionX = calibrationData.Width;
            cam.refResolutionY = calibrationData.Height;
            SceneManager.Instance.CanvasScene.SetActive(true);
        }

        /// <summary>
        /// Nastaveni pozice projektoru z kalibracnich dat.
        /// </summary>
        public void UpdateProjectorTransform()
        {
            GameObject actualKinect = kinect.transform.GetChild(0).gameObject;
            //pricteni posunu k aktualni pozici kinektu
            projector.transform.position = actualKinect.transform.position + calibrationData.Translation;
            //rotacni matice
            Matrix4x4 rotation = calibrationData.Rotation.inverse;
            //prevod matice na Quaternion
            Quaternion rotationQuaternion = Quaternion.LookRotation(rotation.GetColumn(2), rotation.GetColumn(1));
            //aplikovani rotace na rotaci kinektu
            projector.transform.rotation = actualKinect.transform.rotation * rotationQuaternion;
            //prepocitani pozice vsech objektu
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
