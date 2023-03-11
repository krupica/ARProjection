using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Assets.Scripts.AR_Classes
{
    public class CalibrationData
    {
        //public double[,] camInt;
        //public double[,] projInt;
        //public double[] camDist;
        //public double[] projDist;
        public Matrix4x4 rotation;
        public Vector3 translation;

        public CalibrationData (string xmlPath)
        {
            //FillMatrix();
            //return;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            XmlElement rootNode = xmlDoc.DocumentElement;
            XmlNode rotationNode = rootNode.SelectSingleNode("rotation");
            XmlNode translationNode = rootNode.SelectSingleNode("translation");

            double[] rotationData = ReadMatrixData(rotationNode.InnerText);
            double[] translationData = ReadMatrixData(translationNode.InnerText);

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

        public void FillMatrix()
        {
            rotation = new Matrix4x4();
            rotation.SetRow(0, new Vector4((float)9.9995961310770576e-01, (float)8.9872990011441688e-03, (float)2.4701242572249633e-05, 0));
            rotation.SetRow(1, new Vector4((float)-8.9613393813132575e-03, (float)9.9685483491258131e-01, (float)7.8740920161649047e-02, 0));
            rotation.SetRow(2, new Vector4((float)6.8304464003145823e-04, (float)-7.8737961416805169e-02, (float)9.9689511328020131e-01, 0));
            rotation.SetRow(3, new Vector4(0, 0, 0, 1));

            translation = new Vector3((float)8.5737214348908466e+01, (float)-6.3045718819563251e+02, (float)-1.0084398390544085e+02);
        }

        private double[] ReadMatrixData(string matrixData)
        {
            string[] values = matrixData.Split(' ', '\n');
            double[] matrix = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (double.TryParse(values[i], out double value))
                {
                    matrix[i]= value;
                }
                    
            }
            return matrix;
        }
    }    
}