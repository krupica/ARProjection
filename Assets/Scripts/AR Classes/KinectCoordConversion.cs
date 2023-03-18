using System;
using System.Collections.Generic;
using System.Xml;
using Unity.Mathematics;
using UnityEngine;
using Base;
using UnityEditorInternal;
using Packages.Rider.Editor.UnitTesting;

namespace Assets.Scripts.AR_Classes
{
    public class KinectCoordConversion
    {
        //    public static Vector2 LocaltToScreenSpace(Vector3 point, float[] camInt, float[]camDist)
        //    {
        //        testx();
        //        Mat cameraMatrix = ArrayToMat(camInt, 3, 3);
        //        Mat distortionCoefficients = ArrayToMat(camDist, 1, 5);
        //        // convert the 3D point from Unity coordinates to OpenCV coordinates
        //        Point3f[] points = new Point3f[1];
        //        points[0] = new Point3f(point.x, point.y, point.z);


                //        // initialize the rotation and translation vectors to zero
                //        var rvec = new Mat();
                //        var tvec = new Mat();

                //        // project the 3D point onto the 2D image plane using the camera matrix and distortion coefficients
                //        var imagePoints = new Mat();
                //        InputArray inputPoint = InputArray.Create(points);
                //        Cv2.ProjectPoints(inputPoint, rvec, tvec, cameraMatrix, distortionCoefficients, imagePoints);

                //        // get the pixel location of the projected point
                //        var pixelLocation = new Vector2(imagePoints.Get<float>(0, 0), imagePoints.Get<float>(0, 1));
                //        return pixelLocation;
                //    }

                //    public static Mat ArrayToMat(float[] data, int height, int width)
                //    {
                //        Mat cameraMatrix = new Mat(height, width, MatType.CV_32F);
                //        for (int i = 0; i < height; i++)
                //        {
                //            for (int j = 0; j < width; j++)
                //            {
                //                cameraMatrix.Set(i, j, data[i * width + j]);
                //            }
                //        }
                //        return cameraMatrix;
                //    }            
    }
}