using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;

namespace Camlink3_1
{
    // not using this currently, but thought i'd keep it in here for reference if I ever need it.
    public class OleCreateConverter
    {
        // PICTYPE values for the IPictureDisp Type property
        const short _PictureTypeBitmap = 1;
        const short _PictureTypeMetafile = 2;

        // Return a Drawing.Image object representing the supplied IPictureDisp
        // Returns null if it cannot be converted or if the type is not a Bitmap or a Metafile
        public static Image PictureDispToImage(stdole.IPictureDisp pictureDisp)
        {
            Image image = null;
            if (pictureDisp == null)
                return image; // no image to convert
            if (pictureDisp.Type == _PictureTypeBitmap)
            {
                IntPtr paletteHandle = new IntPtr(pictureDisp.hPal);
                IntPtr bitmapHandle = new IntPtr(pictureDisp.Handle);
                image = Image.FromHbitmap(bitmapHandle, paletteHandle);
            }
            else if (pictureDisp.Type == _PictureTypeMetafile)
            {
                IntPtr metafileHandle = new IntPtr(pictureDisp.Handle);
                image = new Metafile(metafileHandle, new WmfPlaceableFileHeader());
            }
            return image;
        }

    }
}
