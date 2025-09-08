using Newtonsoft.Json;

namespace FlightReLive.Core.Cache
{
    public static class JsonHeightmapUtility
    {
        #region METHODS
        public static string Serialize(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            float[][] array = new float[width][];

            for (int x = 0; x < width; x++)
            {
                array[x] = new float[height];
                for (int y = 0; y < height; y++)
                {
                    array[x][y] = heightmap[x, y];
                }
            }

            return JsonConvert.SerializeObject(array, Formatting.None);
        }

        public static float[,] Deserialize(string json)
        {
            float[][] array = JsonConvert.DeserializeObject<float[][]>(json);
            int width = array.Length;
            int height = array[0].Length;
            float[,] heightmap = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    heightmap[x, y] = array[x][y];
                }
            }

            return heightmap;
        }
        #endregion
    }
}