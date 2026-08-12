using UnityEngine;

/// <summary>Villes sélectionnables — positions UI calibrées sur map_france / map_world.</summary>
public static class FranceMapData
{
    public struct MapCity
    {
        public string id;
        public string displayName;
        public float locationMultiplier;
        public float latitude;
        public float longitude;
        public float franceUiX;
        public float franceUiY;
        public float worldUiX;
        public float worldUiY;
        public float worldX;
        public float worldZ;
        public bool isFrance;
    }

    public static readonly MapCity[] Cities =
    {
        // France — calibré pixel par pixel sur map_france.png (côte Méditerranée = bas)
        new MapCity { id = "lille", displayName = "Lille", locationMultiplier = 1.25f, latitude = 50.63f, longitude = 3.06f, franceUiX = 0.48f, franceUiY = 0.86f, worldUiX = 0.505f, worldUiY = 0.70f, worldX = 4f, worldZ = 14f, isFrance = true },
        new MapCity { id = "paris", displayName = "Paris", locationMultiplier = 1.55f, latitude = 48.86f, longitude = 2.35f, franceUiX = 0.45f, franceUiY = 0.72f, worldUiX = 0.505f, worldUiY = 0.685f, worldX = 8f, worldZ = 6f, isFrance = true },
        new MapCity { id = "strasbourg", displayName = "Strasbourg", locationMultiplier = 1.22f, latitude = 48.57f, longitude = 7.75f, franceUiX = 0.72f, franceUiY = 0.70f, worldUiX = 0.525f, worldUiY = 0.685f, worldX = 20f, worldZ = 10f, isFrance = true },
        new MapCity { id = "rennes", displayName = "Rennes", locationMultiplier = 1.12f, latitude = 48.12f, longitude = -1.68f, franceUiX = 0.22f, franceUiY = 0.66f, worldUiX = 0.49f, worldUiY = 0.68f, worldX = -10f, worldZ = 8f, isFrance = true },
        new MapCity { id = "nantes", displayName = "Nantes", locationMultiplier = 1.18f, latitude = 47.22f, longitude = -1.55f, franceUiX = 0.23f, franceUiY = 0.58f, worldUiX = 0.49f, worldUiY = 0.67f, worldX = -14f, worldZ = 2f, isFrance = true },
        new MapCity { id = "lyon", displayName = "Lyon", locationMultiplier = 1.35f, latitude = 45.76f, longitude = 4.84f, franceUiX = 0.58f, franceUiY = 0.48f, worldUiX = 0.515f, worldUiY = 0.665f, worldX = 14f, worldZ = 0f, isFrance = true },
        new MapCity { id = "bordeaux", displayName = "Bordeaux", locationMultiplier = 1.20f, latitude = 44.84f, longitude = -0.58f, franceUiX = 0.28f, franceUiY = 0.40f, worldUiX = 0.495f, worldUiY = 0.655f, worldX = -6f, worldZ = -4f, isFrance = true },
        new MapCity { id = "toulouse", displayName = "Toulouse", locationMultiplier = 1.15f, latitude = 43.60f, longitude = 1.44f, franceUiX = 0.40f, franceUiY = 0.32f, worldUiX = 0.505f, worldUiY = 0.645f, worldX = -8f, worldZ = -12f, isFrance = true },
        // Marseille = golfe du Lion, extrémité est de la côte sud (pas dans la mer / Alpes)
        new MapCity { id = "marseille", displayName = "Marseille", locationMultiplier = 1.40f, latitude = 43.30f, longitude = 5.37f, franceUiX = 0.55f, franceUiY = 0.245f, worldUiX = 0.515f, worldUiY = 0.64f, worldX = 16f, worldZ = -10f, isFrance = true },
        // Nice = Côte d'Azur (côte SE remontée, pas en mer)
        new MapCity { id = "nice", displayName = "Nice", locationMultiplier = 1.38f, latitude = 43.71f, longitude = 7.26f, franceUiX = 0.72f, franceUiY = 0.34f, worldUiX = 0.525f, worldUiY = 0.645f, worldX = 22f, worldZ = -14f, isFrance = true },
        new MapCity { id = "ajaccio", displayName = "Ajaccio", locationMultiplier = 1.10f, latitude = 41.93f, longitude = 8.74f, franceUiX = 0.855f, franceUiY = 0.175f, worldUiX = 0.52f, worldUiY = 0.63f, worldX = 24f, worldZ = -16f, isFrance = true },

        // Monde
        new MapCity { id = "londres", displayName = "Londres", locationMultiplier = 1.60f, latitude = 51.51f, longitude = -0.13f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.50f, worldUiY = 0.64f, worldX = -20f, worldZ = 16f, isFrance = false },
        new MapCity { id = "berlin", displayName = "Berlin", locationMultiplier = 1.45f, latitude = 52.52f, longitude = 13.41f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.535f, worldUiY = 0.655f, worldX = 24f, worldZ = 16f, isFrance = false },
        new MapCity { id = "madrid", displayName = "Madrid", locationMultiplier = 1.30f, latitude = 40.42f, longitude = -3.70f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.475f, worldUiY = 0.60f, worldX = -22f, worldZ = -18f, isFrance = false },
        new MapCity { id = "rome", displayName = "Rome", locationMultiplier = 1.42f, latitude = 41.90f, longitude = 12.50f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.53f, worldUiY = 0.615f, worldX = 18f, worldZ = -8f, isFrance = false },
        new MapCity { id = "istanbul", displayName = "Istanbul", locationMultiplier = 1.40f, latitude = 41.01f, longitude = 28.98f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.56f, worldUiY = 0.62f, worldX = 26f, worldZ = -6f, isFrance = false },
        new MapCity { id = "moscou", displayName = "Moscou", locationMultiplier = 1.35f, latitude = 55.76f, longitude = 37.62f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.58f, worldUiY = 0.70f, worldX = 30f, worldZ = 18f, isFrance = false },
        new MapCity { id = "casablanca", displayName = "Casablanca", locationMultiplier = 1.28f, latitude = 33.57f, longitude = -7.59f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.46f, worldUiY = 0.54f, worldX = -18f, worldZ = -20f, isFrance = false },
        new MapCity { id = "caire", displayName = "Le Caire", locationMultiplier = 1.32f, latitude = 30.04f, longitude = 31.24f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.555f, worldUiY = 0.525f, worldX = 20f, worldZ = -22f, isFrance = false },
        new MapCity { id = "lagos", displayName = "Lagos", locationMultiplier = 1.25f, latitude = 6.52f, longitude = 3.38f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.50f, worldUiY = 0.42f, worldX = 5f, worldZ = -28f, isFrance = false },
        new MapCity { id = "johannesburg", displayName = "Johannesburg", locationMultiplier = 1.22f, latitude = -26.20f, longitude = 28.05f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.555f, worldUiY = 0.28f, worldX = 22f, worldZ = -35f, isFrance = false },
        new MapCity { id = "newyork", displayName = "New York", locationMultiplier = 1.70f, latitude = 40.71f, longitude = -74.01f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.26f, worldUiY = 0.58f, worldX = -30f, worldZ = 10f, isFrance = false },
        new MapCity { id = "losangeles", displayName = "Los Angeles", locationMultiplier = 1.55f, latitude = 34.05f, longitude = -118.24f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.17f, worldUiY = 0.56f, worldX = -40f, worldZ = 5f, isFrance = false },
        new MapCity { id = "mexico", displayName = "Mexico", locationMultiplier = 1.35f, latitude = 19.43f, longitude = -99.13f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.20f, worldUiY = 0.48f, worldX = -35f, worldZ = -5f, isFrance = false },
        new MapCity { id = "saopaulo", displayName = "Sao Paulo", locationMultiplier = 1.45f, latitude = -23.55f, longitude = -46.63f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.32f, worldUiY = 0.30f, worldX = -25f, worldZ = -30f, isFrance = false },
        new MapCity { id = "buenosaires", displayName = "Buenos Aires", locationMultiplier = 1.30f, latitude = -34.60f, longitude = -58.38f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.30f, worldUiY = 0.24f, worldX = -28f, worldZ = -38f, isFrance = false },
        new MapCity { id = "dubai", displayName = "Dubai", locationMultiplier = 1.58f, latitude = 25.20f, longitude = 55.27f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.62f, worldUiY = 0.52f, worldX = 28f, worldZ = -15f, isFrance = false },
        new MapCity { id = "mumbai", displayName = "Mumbai", locationMultiplier = 1.40f, latitude = 19.08f, longitude = 72.88f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.66f, worldUiY = 0.48f, worldX = 32f, worldZ = -12f, isFrance = false },
        new MapCity { id = "bangkok", displayName = "Bangkok", locationMultiplier = 1.35f, latitude = 13.76f, longitude = 100.50f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.74f, worldUiY = 0.45f, worldX = 38f, worldZ = -10f, isFrance = false },
        new MapCity { id = "pekin", displayName = "Pekin", locationMultiplier = 1.50f, latitude = 39.90f, longitude = 116.41f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.78f, worldUiY = 0.60f, worldX = 40f, worldZ = 8f, isFrance = false },
        new MapCity { id = "seoul", displayName = "Seoul", locationMultiplier = 1.48f, latitude = 37.57f, longitude = 126.98f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.81f, worldUiY = 0.58f, worldX = 42f, worldZ = 6f, isFrance = false },
        new MapCity { id = "tokyo", displayName = "Tokyo", locationMultiplier = 1.65f, latitude = 35.68f, longitude = 139.65f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.835f, worldUiY = 0.555f, worldX = 35f, worldZ = 5f, isFrance = false },
        new MapCity { id = "sydney", displayName = "Sydney", locationMultiplier = 1.42f, latitude = -33.87f, longitude = 151.21f, franceUiX = 0.5f, franceUiY = 0.5f, worldUiX = 0.87f, worldUiY = 0.28f, worldX = 45f, worldZ = -32f, isFrance = false },
    };

    public static Vector2 GetUiPos(MapCity city, bool worldMap)
    {
        return worldMap
            ? new Vector2(city.worldUiX, city.worldUiY)
            : new Vector2(city.franceUiX, city.franceUiY);
    }

    public static MapCity? GetById(string id)
    {
        for (int i = 0; i < Cities.Length; i++)
            if (Cities[i].id == id) return Cities[i];
        return null;
    }

    public static float GetPlacementPrice(MapCity city, int ownedCount)
    {
        return GameConstants.BASE_RESTAURANT_PRICE * city.locationMultiplier * (1f + ownedCount * 0.2f);
    }
}
