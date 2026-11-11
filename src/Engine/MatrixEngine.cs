using System;
using System.Globalization;
using System.Text;

namespace TitanCalc.Engine
{
    /// <summary>
    /// Matrix engine for arbitrary M x N matrices, determinants, inverses, and linear systems.
    /// </summary>
    public class MatrixEngine
    {
        private readonly int _rows;
        private readonly int _cols;
        private readonly double[,] _data;

        public int Rows { get { return _rows; } }
        public int Cols { get { return _cols; } }

        public MatrixEngine(int rows, int cols)
        {
            if (rows <= 0 || cols <= 0)
                throw new ArgumentException("Matrix dimensions must be positive.");
            _rows = rows;
            _cols = cols;
            _data = new double[rows, cols];
        }

        public MatrixEngine(double[,] data)
        {
            _rows = data.GetLength(0);
            _cols = data.GetLength(1);
            _data = (double[,])data.Clone();
        }

        public double this[int r, int c]
        {
            get { return _data[r, c]; }
            set { _data[r, c] = value; }
        }

        public bool IsSquare { get { return _rows == _cols; } }

        public static MatrixEngine Identity(int size)
        {
            MatrixEngine m = new MatrixEngine(size, size);
            for (int i = 0; i < size; i++)
                m[i, i] = 1.0;
            return m;
        }

        public MatrixEngine Transpose()
        {
            MatrixEngine res = new MatrixEngine(_cols, _rows);
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                    res[c, r] = _data[r, c];
            return res;
        }

        public static MatrixEngine operator +(MatrixEngine a, MatrixEngine b)
        {
            if (a._rows != b._rows || a._cols != b._cols)
                throw new InvalidOperationException("Matrix dimensions must match for addition.");
            MatrixEngine res = new MatrixEngine(a._rows, a._cols);
            for (int r = 0; r < a._rows; r++)
                for (int c = 0; c < a._cols; c++)
                    res[r, c] = a[r, c] + b[r, c];
            return res;
        }

        public static MatrixEngine operator -(MatrixEngine a, MatrixEngine b)
        {
            if (a._rows != b._rows || a._cols != b._cols)
                throw new InvalidOperationException("Matrix dimensions must match for subtraction.");
            MatrixEngine res = new MatrixEngine(a._rows, a._cols);
            for (int r = 0; r < a._rows; r++)
                for (int c = 0; c < a._cols; c++)
                    res[r, c] = a[r, c] - b[r, c];
            return res;
        }

        public static MatrixEngine operator *(MatrixEngine a, double scalar)
        {
            MatrixEngine res = new MatrixEngine(a._rows, a._cols);
            for (int r = 0; r < a._rows; r++)
                for (int c = 0; c < a._cols; c++)
                    res[r, c] = a[r, c] * scalar;
            return res;
        }

        public static MatrixEngine operator *(MatrixEngine a, MatrixEngine b)
        {
            if (a._cols != b._rows)
                throw new InvalidOperationException(string.Format("Cannot multiply {0}x{1} by {2}x{3} matrix.", a._rows, a._cols, b._rows, b._cols));
            MatrixEngine res = new MatrixEngine(a._rows, b._cols);
            for (int r = 0; r < a._rows; r++)
            {
                for (int c = 0; c < b._cols; c++)
                {
                    double sum = 0.0;
                    for (int k = 0; k < a._cols; k++)
                        sum += a[r, k] * b[k, c];
                    res[r, c] = sum;
                }
            }
            return res;
        }

        public double Determinant()
        {
            if (!IsSquare)
                throw new InvalidOperationException("Determinant only exists for square matrices.");

            int n = _rows;
            double[,] a = (double[,])_data.Clone();
            double det = 1.0;

            for (int i = 0; i < n; i++)
            {
                int pivot = i;
                for (int j = i + 1; j < n; j++)
                {
                    if (Math.Abs(a[j, i]) > Math.Abs(a[pivot, i]))
                        pivot = j;
                }

                if (Math.Abs(a[pivot, i]) < 1e-14)
                    return 0.0;

                if (pivot != i)
                {
                    for (int k = 0; k < n; k++)
                    {
                        double tmp = a[i, k];
                        a[i, k] = a[pivot, k];
                        a[pivot, k] = tmp;
                    }
                    det = -det;
                }

                det *= a[i, i];
                for (int j = i + 1; j < n; j++)
                {
                    double factor = a[j, i] / a[i, i];
                    for (int k = i; k < n; k++)
                        a[j, k] -= factor * a[i, k];
                }
            }

            return Math.Abs(det) < 1e-12 ? 0.0 : det;
        }

        public MatrixEngine Inverse()
        {
            if (!IsSquare)
                throw new InvalidOperationException("Inverse only exists for square matrices.");

            int n = _rows;
            double[,] a = (double[,])_data.Clone();
            double[,] inv = new double[n, n];
            for (int i = 0; i < n; i++)
                inv[i, i] = 1.0;

            for (int i = 0; i < n; i++)
            {
                int pivot = i;
                for (int j = i + 1; j < n; j++)
                {
                    if (Math.Abs(a[j, i]) > Math.Abs(a[pivot, i]))
                        pivot = j;
                }

                if (Math.Abs(a[pivot, i]) < 1e-14)
                    throw new InvalidOperationException("Matrix is singular and cannot be inverted.");

                if (pivot != i)
                {
                    for (int k = 0; k < n; k++)
                    {
                        double tA = a[i, k]; a[i, k] = a[pivot, k]; a[pivot, k] = tA;
                        double tI = inv[i, k]; inv[i, k] = inv[pivot, k]; inv[pivot, k] = tI;
                    }
                }

                double div = a[i, i];
                for (int k = 0; k < n; k++)
                {
                    a[i, k] /= div;
                    inv[i, k] /= div;
                }

                for (int j = 0; j < n; j++)
                {
                    if (j != i)
                    {
                        double factor = a[j, i];
                        for (int k = 0; k < n; k++)
                        {
                            a[j, k] -= factor * a[i, k];
                            inv[j, k] -= factor * inv[i, k];
                        }
                    }
                }
            }

            return new MatrixEngine(inv);
        }

        public double Trace()
        {
            if (!IsSquare)
                throw new InvalidOperationException("Trace is only defined for square matrices.");
            double tr = 0.0;
            for (int i = 0; i < _rows; i++)
                tr += _data[i, i];
            return tr;
        }

        /// <summary>
        /// Solves linear system A * X = B using Gaussian elimination with partial pivoting.
        /// </summary>
        public static double[] SolveLinearSystem(MatrixEngine A, double[] B)
        {
            if (!A.IsSquare)
                throw new ArgumentException("Matrix A must be square.");
            if (A._rows != B.Length)
                throw new ArgumentException("Dimensions of matrix A and vector B must match.");

            int n = A._rows;
            double[,] a = (double[,])A._data.Clone();
            double[] b = (double[])B.Clone();

            for (int i = 0; i < n; i++)
            {
                int pivot = i;
                for (int j = i + 1; j < n; j++)
                    if (Math.Abs(a[j, i]) > Math.Abs(a[pivot, i]))
                        pivot = j;

                if (Math.Abs(a[pivot, i]) < 1e-14)
                    throw new InvalidOperationException("The system is singular or has infinite solutions.");

                if (pivot != i)
                {
                    for (int k = 0; k < n; k++)
                    {
                        double tmp = a[i, k];
                        a[i, k] = a[pivot, k];
                        a[pivot, k] = tmp;
                    }
                    double tb = b[i]; b[i] = b[pivot]; b[pivot] = tb;
                }

                for (int j = i + 1; j < n; j++)
                {
                    double factor = a[j, i] / a[i, i];
                    b[j] -= factor * b[i];
                    for (int k = i; k < n; k++)
                        a[j, k] -= factor * a[i, k];
                }
            }

            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = b[i];
                for (int j = i + 1; j < n; j++)
                    sum -= a[i, j] * x[j];
                x[i] = sum / a[i, i];
            }

            return x;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int r = 0; r < _rows; r++)
            {
                if (r > 0) sb.Append("; ");
                sb.Append("[");
                for (int c = 0; c < _cols; c++)
                {
                    if (c > 0) sb.Append(", ");
                    sb.Append(_data[r, c].ToString("G6", CultureInfo.InvariantCulture));
                }
                sb.Append("]");
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}
