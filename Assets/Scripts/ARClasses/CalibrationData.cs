using Base;
using IO.Swagger.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.ARClasses
{
    public class CalibrationData
    {
        public float CamFx
        {
            get { return CamMatrix[0,0]; }
            set { CamMatrix[0,0] = value;}
        }

        public float CamFy
        {
            get { return CamMatrix[1,1]; }
            set { CamMatrix[1,1] = value;}
        }

        public float CamCx
        {
            get { return CamMatrix[0,2]; }
            set { CamMatrix[0,2] = value; }
        }

        public float CamCy
        {
            get { return CamMatrix[1,2]; }
            set { CamMatrix[1,2] = value;}            
        }

        public Matrix4x4 CamMatrix;

        //TODO nastavit šířku projektoru podle kalibračních dat
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
        public Matrix4x4 Extrinsic
        {
            get { return extrinsic; }
            set { extrinsic = value;}
        }

        public Matrix4x4 Rotation { get; set; }

        public Vector3 Translation { get; private set; }

        public IEnumerable<decimal> CamDist {  get; private set; }

        public Matrix4x4 ProjInt { get; private set; }

        private int[] imgShape;
        private Matrix4x4 extrinsic;
        

        public CalibrationData (string xmlPath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            XmlElement rootNode = xmlDoc.DocumentElement;
            XmlNode rotationNode = rootNode.SelectSingleNode("rotation");
            XmlNode translationNode = rootNode.SelectSingleNode("translation");
            XmlNode imgShapeNode = rootNode.SelectSingleNode("img_shape");
            XmlNode projIntNode = rootNode.SelectSingleNode("proj_int");

            float[] imgShapeData = ReadMatrixData(imgShapeNode.InnerText);
            float[] rotationData = ReadMatrixData(rotationNode.InnerText);
            float[] translationData = ReadMatrixData(translationNode.InnerText);
            float[] projIntData = ReadMatrixData(projIntNode.InnerText);

            imgShape = new int[] { (int)imgShapeData[1], (int)imgShapeData[0] };

            ProjInt = new Matrix4x4(
                new Vector4(projIntData[0], projIntData[1], projIntData[2], 0f) * 0.001f,
                new Vector4(projIntData[3], projIntData[4], projIntData[5], 0f) * 0.001f,
                new Vector4(projIntData[6], projIntData[7], projIntData[8], 0f) * 0.001f,
                new Vector4(0f, 0f, 0f, 1f)
                );
            ProjInt = ProjInt ;

            Rotation = new Matrix4x4(
                new Vector4(rotationData[0], rotationData[1], rotationData[2], 0f),
                new Vector4(rotationData[3], rotationData[4], rotationData[5], 0f),
                new Vector4(rotationData[6], rotationData[7], rotationData[8], 0f),
                new Vector4(0f, 0f, 0f, 1f)
            );

            Translation = new Vector3(
                translationData[0],
                translationData[2],
                translationData[1]
            );
            Translation = Translation * 0.001f;


            extrinsic = Matrix4x4.identity;
            extrinsic.SetRow(0, new Vector4(Rotation[0, 0], Rotation[0, 1], Rotation[0, 2], Translation[0]));
            extrinsic.SetRow(1, new Vector4(Rotation[1, 0], Rotation[1, 1], Rotation[1, 2], Translation[1]));
            extrinsic.SetRow(2, new Vector4(Rotation[2, 0], Rotation[2, 1], Rotation[2, 2], Translation[2]));
        }

        public void SetCamCalibFromParams(CameraParameters camParams)
        {
            //File.WriteAllText("KinectCamData.json", camParams.ToJson());
            CamMatrix = Matrix4x4.identity;
            CamCx = (float)camParams.Cx;
            CamCy = (float)camParams.Cy;
            CamFx = (float)camParams.Fx;
            CamFy = (float)camParams.Fy;
            CamDist = camParams.DistCoefs;
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

            try
            {
                CameraParameters camParams = await WebsocketManager.Instance.GetCameraColorParameters(kinectId);
                SetCamCalibFromParams(camParams);
                await WebsocketManager.Instance.WriteUnlock(kinectId);
            }
            catch (RequestFailedException e)
            {
                await WebsocketManager.Instance.WriteUnlock(kinectId);
            }
        }

        private float[] ReadMatrixData(string matrixData)
        {
            int count = 0;
            string[] values = matrixData.Split(' ', '\n', System.StringSplitOptions.RemoveEmptyEntries);
            float[] matrix = new float[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                //převzato z https://stackoverflow.com/questions/64639/convert-from-scientific-notation-string-to-float-in-c-sharp
                string curentValue = values[i].TrimEnd('.');
                if (float.TryParse(curentValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    matrix[count]= (float)value;
                    count++;
                }
                    
            }
            return matrix;
        }
    }    
}