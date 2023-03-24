using Base;
using IO.Swagger.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.AR_Classes
{
    public class CalibrationData
    {
        public bool Calibrated { get; set; }
        public float CamFx
        {
            get { return CamMatrix[0]; }
            set { CamMatrix[0] = value;}
        }

        public float CamFy
        {
            get { return CamMatrix[4]; }
            set { CamMatrix[4] = value;}
        }

        public float CamCx
        {
            get { return CamMatrix[2]; }
            set { CamMatrix[2] = value; }
        }

        public float CamCy
        {
            get { return CamMatrix[5]; }
            set { CamMatrix[5] = value;}            
        }

        public Matrix4x4 CamMatrix;

        public int Width
        {
            get { return imgShape[0]; }
            set { imgShape[0] = value;}
        }

        public int Height
        {
            get { return imgShape[1]; }
            set { imgShape[1] = value;}
        }

        public Transform KinectPosition { get; set; }

        public Matrix4x4 Rotation { get; private set; }

        public Vector3 Translation { get; private set; }

        public IEnumerable<decimal> CamDist {  get; private set; }

        private int[] imgShape;
        private float[] projInt;
        private float[] projDist;
        
        private Matrix4x4 extrinsic;

        public CalibrationData (string xmlPath)
        {
            Calibrated = false;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            XmlElement rootNode = xmlDoc.DocumentElement;
            XmlNode rotationNode = rootNode.SelectSingleNode("rotation");
            XmlNode translationNode = rootNode.SelectSingleNode("translation");
            XmlNode imgShapeNode = rootNode.SelectSingleNode("img_shape");
            XmlNode projIntNode = rootNode.SelectSingleNode("cam_dist");
            XmlNode camDistNode = rootNode.SelectSingleNode("proj_int");

            float[] imgShapeData = ReadMatrixData(imgShapeNode.InnerText);
            float[] rotationData = ReadMatrixData(rotationNode.InnerText);
            float[] translationData = ReadMatrixData(translationNode.InnerText);
            float[] projIntData = ReadMatrixData(projIntNode.InnerText);
            float[] camDistData = ReadMatrixData(camDistNode.InnerText);

            imgShape = new int[] { (int)imgShapeData[1], (int)imgShapeData[0] };
            projInt = new float[9] { projIntData[0], projIntData[1], projIntData[2] ,
                 projIntData[3], projIntData[4], projIntData[5],
                projIntData[6], projIntData[7], projIntData[8] };

            CamMatrix = Matrix4x4.identity;

            Rotation = new Matrix4x4(
                new Vector4(rotationData[0], rotationData[1], rotationData[2], 0f),
                new Vector4(rotationData[3], rotationData[4], rotationData[5], 0f),
                new Vector4(rotationData[6], rotationData[7], rotationData[8], 0f),
                new Vector4(0f, 0f, 0f, 1f)
            );

            Translation = new Vector3(
                translationData[0],
                translationData[1],
                translationData[2]
            );

            extrinsic = Matrix4x4.identity;
            extrinsic.SetRow(0, new Vector4(Rotation[0, 0], Rotation[0, 1], Rotation[0, 2], Translation[0]));
            extrinsic.SetRow(1, new Vector4(Rotation[1, 0], Rotation[1, 1], Rotation[1, 2], Translation[1]));
            extrinsic.SetRow(2, new Vector4(Rotation[2, 0], Rotation[2, 1], Rotation[2, 2], Translation[2]));
        }

        public void SetCamCalibFromParams(CameraParameters camParams)
        {
            Debug.Log(camParams.ToString());
            var x = camParams.ToString();
            CamCx = (float)camParams.Cx;
            CamCy = (float)camParams.Cy;
            CamFx = (float)camParams.Fx;
            CamFy = (float)camParams.Fy;
            CamDist = camParams.DistCoefs;
            Calibrated = true;
        }

        public async Task GetCameraParameters(string kinectId)
        {
            try
            {
                await WebsocketManager.Instance.WriteLock(kinectId, false);
            }
            catch(RequestFailedException e)
            {
                await WebsocketManager.Instance.WriteUnlock(kinectId);
                await WebsocketManager.Instance.WriteLock(kinectId, false);
            }
            CameraParameters camParams = await WebsocketManager.Instance.GetCameraColorParameters(kinectId);
            await WebsocketManager.Instance.WriteUnlock(kinectId);

            SetCamCalibFromParams(camParams);
        }

        private float[] ReadMatrixData(string matrixData)
        {
            int count = 0;
            string[] values = matrixData.Split(' ', '\n');
            float[] matrix = new float[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (float.TryParse(values[i], out float value))
                {
                    matrix[count]= value;
                    count++;
                }
                    
            }
            return matrix;
        }

        //public void FillMatrix()
        //{
        //    rotation = new Matrix4x4();
        //    rotation.SetRow(0, new Vector4(9.9995961310770576e-01, 8.9872990011441688e-03, 2.4701242572249633e-05, 0));
        //    rotation.SetRow(1, new Vector4(-8.9613393813132575e-03, 9.9685483491258131e-01, 7.8740920161649047e-02, 0));
        //    rotation.SetRow(2, new Vector4(6.8304464003145823e-04, -7.8737961416805169e-02, 9.9689511328020131e-01, 0));
        //    rotation.SetRow(3, new Vector4(0, 0, 0, 1));

        //    translation = new Vector3(8.5737214348908466e+01, -6.3045718819563251e+02, -1.0084398390544085e+02);
        //}
    }    
}