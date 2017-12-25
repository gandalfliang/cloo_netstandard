using System;

namespace demo
{
    public class MatrixMultiple
    {
        private readonly int Rank;

        private readonly int[,] matrixA;
        int[,] sum;

        public MatrixMultiple(int rank)
        {
            Rank=rank;
            matrixA=new int[rank,rank];
            sum=new int[rank,rank];
            InitArrays();
        }

        private void InitArrays()
        {
            var rnd=new Random();
            for (int i = 0; i < Rank; i++)
                for (int j = 0; j < Rank; j++)
                    matrixA[i,j]=rnd.Next(1,100);
        }

        public void RegularMul()
        {
            for (int i = 0; i < Rank; i++)
                for (int j = 0; j < Rank; j++)
                    for (int k = 0; k < Rank; k++)
                        sum[i,j] += matrixA[i,k] * matrixA[k,j];
        }

        public void OpenClMul()
        {
            
        }
    }
}