using System;

namespace DATN
{
    public class DHParameter
    {
        public double Theta { get; set; }
        public double Alpha { get; set; }
        public double R { get; set; }
        public double D { get; set; }

        public DHParameter(double theta, double alpha, double r, double d)
        {
            Theta = theta;
            Alpha = alpha;
            R = r;
            D = d;
        }

        public static double[,] CalculateTransformationMatrix(double theta, double alpha, double r, double d)
        {
            theta = theta * Math.PI / 180.0; // Convert to radians
            alpha = alpha * Math.PI / 180.0; // Convert to radians

            double[,] transformationMatrix = new double[4, 4]
            {
                { Math.Cos(theta), -Math.Sin(theta) * Math.Cos(alpha), Math.Sin(theta) * Math.Sin(alpha), r * Math.Cos(theta) },
                { Math.Sin(theta), Math.Cos(theta) * Math.Cos(alpha), -Math.Cos(theta) * Math.Sin(alpha), r * Math.Sin(theta) },
                { 0, Math.Sin(alpha), Math.Cos(alpha), d },
                { 0, 0, 0, 1 }
            };

            return transformationMatrix;
        }

        public static double[,] MultiplyMatrices(double[,] matrixA, double[,] matrixB)
        {
            int rowsA = matrixA.GetLength(0);
            int colsA = matrixA.GetLength(1);
            int rowsB = matrixB.GetLength(0);
            int colsB = matrixB.GetLength(1);

            if (colsA != rowsB)
                throw new Exception("Matrix dimensions are not valid for multiplication.");

            double[,] result = new double[rowsA, colsB];

            for (int i = 0; i < rowsA; i++)
            {
                for (int j = 0; j < colsB; j++)
                {
                    result[i, j] = 0;
                    for (int k = 0; k < colsA; k++)
                    {
                        result[i, j] += matrixA[i, k] * matrixB[k, j];
                    }
                }
            }

            return result;
        }
    }
}
