using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Collections;
using System.IO;

namespace RadProject
{
    public static class RadanProjectExtension
    {
        public static IEnumerable<RadanPart> Values(this RadanParts parts)
        {
            if (parts == null)
                throw new ArgumentNullException();

            foreach (RadanPart p in parts.Part)
            {
                yield return p;
            }
        }

        public static int Count(this RadanParts parts)
        {
            int i = 0;
            foreach (RadanPart p in parts.Part)
            {
                i++;
            }
            return i;
        }

        public static bool AddPart(this RadanProject radanProject, RadanPart radanPart)
        {
            try
            {
                radanPart.ID = radanProject.Parts.NextID;
                radanProject.Parts.Add(radanPart);
                radanProject.Parts.NextID++;

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public static bool SaveData(this RadanProject radanProject, string fileName)
        {
            try
            {
                string path = fileName;
                System.IO.Stream stream = null;
                stream = System.IO.File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                using (StreamWriter Writer = new StreamWriter(stream))
                {
                    XmlSerializer prjSerializer = new XmlSerializer(typeof(RadanProject));

                    stream = System.IO.File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    prjSerializer.Serialize(stream, radanProject);

                    stream.Close();
                    Writer.Close();
                }
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        public static RadanProject LoadData(this RadanProject radanProject, string fileName)
        {
            try
            {
                System.IO.Stream stream = System.IO.File.Open(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                stream.Flush();

                using (StreamReader reader = new StreamReader(stream))
                {
                    XmlSerializer prjSerializer = new XmlSerializer(typeof(RadanProject));
                    radanProject = (RadanProject)prjSerializer.Deserialize(reader);
                }
                return radanProject;
            }
            catch (Exception ex)
            {
                return null;
            }
        }




    }
}
