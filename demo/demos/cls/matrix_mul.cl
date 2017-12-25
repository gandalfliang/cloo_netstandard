kernel void MatrixMul(
	global write_only int* result,
	global read_only int* matrix,
	int rank)
{
    int gx=get_global_id(0);
    int gy=get_global_id(1);
    int loc=gx*rank+gy;
    for(int i=0;i<rank;i++)
    {
        int leftLoc=gx*rank+i;
        int rightLoc=i*rank+gy;
        result[loc]+=matrix[leftLoc]*matrix[rightLoc];
    }
}