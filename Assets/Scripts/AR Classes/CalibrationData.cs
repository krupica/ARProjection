using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Assets.Scripts.AR_Classes
{
    public class CalibrationData
    {
        public int[] imgShape;
        public float[] camInt;
        public float[] projInt;
        public float[] camDist;
        public float[] projDist;
        public Matrix4x4 rotation;
        public Vector3 translation;

        public CalibrationData (string xmlPath)
        {           
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            XmlElement rootNode = xmlDoc.DocumentElement;
            XmlNode rotationNode = rootNode.SelectSingleNode("rotation");
            XmlNode translationNode = rootNode.SelectSingleNode("translation");
            XmlNode imgShapeNode = rootNode.SelectSingleNode("img_shape");
            XmlNode camIntNode = rootNode.SelectSingleNode("cam_int");
            XmlNode projIntNode = rootNode.SelectSingleNode("cam_dist");
            XmlNode camDistNode = rootNode.SelectSingleNode("proj_int");
            XmlNode projDistNode = rootNode.SelectSingleNode("proj_dist");

            float[] imgShapeData = ReadMatrixData(imgShapeNode.InnerText);
            float[] rotationData = ReadMatrixData(rotationNode.InnerText);
            float[] translationData = ReadMatrixData(translationNode.InnerText);
            float[] camIntData = ReadMatrixData(camIntNode.InnerText);
            float[] projIntData = ReadMatrixData(projIntNode.InnerText);
            float[] camDistData = ReadMatrixData(camDistNode.InnerText);
            float[] projDistData = ReadMatrixData(projDistNode.InnerText);

            imgShape = new int[] { (int)imgShapeData[0], (int)imgShapeData[1] };
            camInt = new float[9] { camIntData[0], camIntData[1], camIntData[2] ,
                 camIntData[3], camIntData[4], camIntData[5],
                camIntData[6], camIntData[7], camIntData[8]};
            camDist = new float[5] { camDistData[0], camDistData[1], camDistData[2], camDistData[3], camDistData[4] };
            projInt = new float[9] { projIntData[0], projIntData[1], projIntData[2] ,
                 projIntData[3], projIntData[4], projIntData[5],
                projIntData[6], projIntData[7], projIntData[8] };
            projDist = new float[5] { projDistData[0], projDistData[1], projDistData[2], projDistData[3], projDistData[4] };


            rotation = new Matrix4x4(
                new Vector4((float)rotationData[0], (float)rotationData[1], (float)rotationData[2], 0f),
                new Vector4((float)rotationData[3], (float)rotationData[4], (float)rotationData[5], 0f),
                new Vector4((float)rotationData[6], (float)rotationData[7], (float)rotationData[8], 0f),
                new Vector4(0f, 0f, 0f, 1f)
            );

            translation = new Vector3(
                (float)translationData[0],
                (float)translationData[1],
                (float)translationData[2]
            );
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

        public void FillMatrix()
        {
            rotation = new Matrix4x4();
            rotation.SetRow(0, new Vector4((float)9.9995961310770576e-01, (float)8.9872990011441688e-03, (float)2.4701242572249633e-05, 0));
            rotation.SetRow(1, new Vector4((float)-8.9613393813132575e-03, (float)9.9685483491258131e-01, (float)7.8740920161649047e-02, 0));
            rotation.SetRow(2, new Vector4((float)6.8304464003145823e-04, (float)-7.8737961416805169e-02, (float)9.9689511328020131e-01, 0));
            rotation.SetRow(3, new Vector4(0, 0, 0, 1));

            translation = new Vector3((float)8.5737214348908466e+01, (float)-6.3045718819563251e+02, (float)-1.0084398390544085e+02);
        }
    }    
}