using System;
using UnityEngine;

[Serializable]
public class LocalGenerationProfileSet
{
    public GenerationResponse[] profiles;
}

public static class LocalGenerationProfileDatabase
{
    public static GenerationResponse TryGetProfile(TextAsset jsonAsset, string requestedColor)
    {
        if (jsonAsset == null)
        {
            Debug.LogError("[LocalGenerationProfileDatabase] Local profiles JSON asset is not assigned.");
            return null;
        }

        return TryGetProfile(jsonAsset.text, requestedColor);
    }

    public static GenerationResponse TryGetProfile(string json, string requestedColor)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("[LocalGenerationProfileDatabase] Local profiles JSON is empty.");
            return null;
        }

        string color = NormalizeColor(requestedColor);

        try
        {
            LocalGenerationProfileSet profileSet = JsonUtility.FromJson<LocalGenerationProfileSet>(json);

            if (profileSet == null || profileSet.profiles == null || profileSet.profiles.Length == 0)
            {
                Debug.LogError("[LocalGenerationProfileDatabase] Local profiles JSON does not contain any profiles.");
                return null;
            }

            GenerationResponse fallbackProfile = null;

            foreach (GenerationResponse profile in profileSet.profiles)
            {
                if (profile == null)
                    continue;

                if (fallbackProfile == null)
                    fallbackProfile = profile;

                string profileColor = NormalizeColor(profile.sphereColor);

                if (profileColor == color)
                    return CloneProfile(profile, color);
            }

            if (fallbackProfile != null)
            {
                Debug.LogWarning($"[LocalGenerationProfileDatabase] Local profile for color '{color}' was not found. First local profile will be used as fallback.");
                return CloneProfile(fallbackProfile, color);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[LocalGenerationProfileDatabase] Failed to parse local profiles JSON: {exception.Message}");
        }

        return null;
    }

    private static GenerationResponse CloneProfile(GenerationResponse source, string requestedColor)
    {
        string json = JsonUtility.ToJson(source);
        GenerationResponse clone = JsonUtility.FromJson<GenerationResponse>(json);

        if (clone != null)
        {
            clone.sphereColor = requestedColor;

            if (clone.seed <= 0)
                clone.seed = UnityEngine.Random.Range(1000, 999999999);
        }

        return clone;
    }

    private static string NormalizeColor(string color)
    {
        return string.IsNullOrWhiteSpace(color)
            ? "blue"
            : color.Trim().ToLowerInvariant();
    }
}
