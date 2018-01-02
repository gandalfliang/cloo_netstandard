inline int2 transform_pos(int centerX,int centerY,int matrixWidth,int radius,int index)
{
    int x=index%matrixWidth;
    int offsetX=x-(radius+1);
    int y=index/matrixWidth;
    int offsetY=radius-y;
    return (int2)(centerX+offsetX,centerY-offsetY);
};

const sampler_t sampler_img=CLK_NORMALIZED_COORDS_FALSE|CLK_ADDRESS_CLAMP_TO_EDGE;

kernel void gaussian_blur(
    read_only image2d_t src,
    global write_only char* dst,
    global read_only float* matrix,
    read_only int radius,
	read_only int width)
{
    int x=get_global_id(0);
    int y=get_global_id(1);

    float sumR,sumG,sumB,sumA;
    int matrixWidth=radius*2+1;
    int matrix_size=pow((float)matrixWidth,2.0f);//which should be the size fo matrix
    for(int i=0;i<matrix_size;i++)
    {
        int2 pix=transform_pos(x,y,matrixWidth,radius,i);
        uint4 rgba = read_imageui(src,sampler_img,pix);
        sumR+=rgba.x*matrix[i];
        sumG+=rgba.y*matrix[i];
        sumB+=rgba.z*matrix[i];
		sumA+=rgba.w*matrix[i];
    }
   
	int loc=y*width*4+x*4;
	dst[loc]=sumR;
	dst[loc+1]=sumG;
	dst[loc+2]=sumB;
	dst[loc+3]=sumA;
}



