int2 transform_pos(int centerX,int centerY,int matrixWidth,int radius,int index)
{
    int x=index%matrixWidth;
    int offsetX=x-(radius+1);
    int y=index/matrixWidth;
    int offsetY=y-(radius+1);
    return (int2)(centerX+offsetX,centerY+offsetY);
};

const sampler_t sampler_img=CLK_NORMALIZED_COORDS_TRUE|CLK_ADDRESS_CLAMP_TO_EDGE|CLK_FILTER_NEAREST;

kernel void gaussian_blur(
    read_only image2d_t src,
    write_only image2d_t dst,
    global read_only float* matrix,
    read_only int radius)
{
    int x=get_global_id(0);
    int y=get_global_id(1);

    float sumR,sumG,sumB;
    int matrixWidth=radius*2+1;
    int matrix_size=pow(matrixWidth,2);//which should be the size fo matrix
    for(int i=0;i<matrix_size;i++)
    {
        int2 pix=transform_pos(x,y,matrixWidth,radius,i);
        int4 rgba = read_imageui(src,sampler_img,pix);
        sumR+=rgba.x*matrix[i];
        sumG+=rgba.y*matrix[i];
        sumB+=rgba.z*matrix[i];
    }
    write_imageui(dst,(int2)(x,y),(int4)(sumR,sumG,sumB,1));
}