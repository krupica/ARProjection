using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace Assets.Scripts.ARClasses
{
    public class CalibrationData
    {
        public int Width
        {
            get { return imgShape[0]; }
        }

        public int Height
        {
            get { return imgShape[1]; }
        }

        public float K1
        {
            get { return ProjDist[0]; }
        }

        public float K2
        {
            get { return ProjDist[1]; }
        }

        public float K3
        {
            get { return ProjDist[4]; }
        }

        public float P1
        {
            get { return ProjDist[2]; }
        }

        public float P2
        {
            get { return ProjDist[3]; }
        }

        public Matrix4x4 Rotation { get; set; }

        public Vector3 Translation { get; private set; }

        public Matrix4x4 ProjInt { get; private set; }

        //k1,k2,p1,p2,k3
        private List<float> ProjDist { get; set; }

        private int[] imgShape;

        public CalibrationData (string xmlPath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            XmlElement rootNode = xmlDoc.DocumentElement;
            XmlNode rotationNode = rootNode.SelectSingleNode("rotation");
            XmlNode translationNode = rootNode.SelectSingleNode("translation");
            XmlNode imgShapeNode = rootNode.SelectSingleNode("img_shape");
            XmlNode projIntNode = rootNode.SelectSingleNode("proj_int");
            XmlNode proj_dist = rootNode.SelectSingleNode("proj_dist");
            

            float[] imgShapeData = ReadMatrixData(imgShapeNode.InnerText);
            float[] rotationData = ReadMatrixData(rotationNode.InnerText);
            float[] translationData = ReadMatrixData(translationNode.InnerText);
            float[] projIntData = ReadMatrixData(projIntNode.InnerText);
            float[] proj_distData = ReadMatrixData(proj_dist.InnerText);
            
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

            ProjDist = new List<float>(proj_distData);
            for( int i = 0; i < proj_distData.Length; i++)
            {
                ProjDist[i] *= 0.001f;
            }
        }

        private float[] ReadMatrixData(string matrixData)
        {
            int count = 0;
            string[] values = matrixData.Split(' ', '\n', System.StringSplitOptions.RemoveEmptyEntries);
            float[] matrix = new float[values.Length-1];
            for (int i = 0; i < values.Length; i++)
            {
                //převzato z https://stackoverflow.com/questions/64639/convert-from-scientific-notation-string-to-float-in-c-sharp
                string curentValue = values[i].TrimEnd('.');
                if (float.TryParse(curentValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    matrix[count]= value;
                    count++;
                }
            }
            return matrix;
        }
    }    
}