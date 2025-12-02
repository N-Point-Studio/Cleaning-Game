using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data structure untuk save/load ContentSwitcher completion status
/// </summary>
[System.Serializable]
public class SaveData
{
    [Header("Game Progress")]
    public List<CompletedObject> completedObjects = new List<CompletedObject>();
    public string lastSaveTime;
    public int saveVersion = 1;

    [Header("UI Progress")]
    public bool introTransitionShown = false;

    public SaveData()
    {
        completedObjects = new List<CompletedObject>();
        lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Check if specific object already completed
    /// </summary>
    public bool IsObjectCompleted(string objectName, ObjectType objectType)
    {
        foreach (var completedObj in completedObjects)
        {
            if (completedObj.objectName == objectName && completedObj.objectType == objectType)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Add completed object to save data
    /// </summary>
    public void AddCompletedObject(string objectName, ObjectType objectType, ChapterType chapterType, Vector3 position)
    {
        // Check if already exists
        if (IsObjectCompleted(objectName, objectType))
        {
            return;
        }

        var completedObject = new CompletedObject
        {
            objectName = objectName,
            objectType = objectType,
            chapterType = chapterType,
            position = position,
            completedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        completedObjects.Add(completedObject);
        lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Remove specific completed object
    /// </summary>
    public bool RemoveCompletedObject(string objectName, ObjectType objectType)
    {
        for (int i = completedObjects.Count - 1; i >= 0; i--)
        {
            var obj = completedObjects[i];
            if (obj.objectName == objectName && obj.objectType == objectType)
            {
                completedObjects.RemoveAt(i);
                lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Clear all completed objects
    /// </summary>
    public void ClearAllProgress()
    {
        completedObjects.Clear();
        introTransitionShown = false;
        lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Get total completed objects count
    /// </summary>
    public int GetCompletedCount()
    {
        return completedObjects.Count;
    }

    /// <summary>
    /// Get completed objects by chapter
    /// </summary>
    public List<CompletedObject> GetCompletedObjectsByChapter(ChapterType chapterType)
    {
        List<CompletedObject> result = new List<CompletedObject>();
        foreach (var obj in completedObjects)
        {
            if (obj.chapterType == chapterType)
            {
                result.Add(obj);
            }
        }
        return result;
    }

    /// <summary>
    /// Check if intro transition has been shown
    /// </summary>
    public bool IsIntroTransitionShown()
    {
        return introTransitionShown;
    }

    /// <summary>
    /// Mark intro transition as shown
    /// </summary>
    public void SetIntroTransitionShown(bool shown = true)
    {
        introTransitionShown = shown;
        lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

/// <summary>
/// Individual completed object data
/// </summary>
[System.Serializable]
public class CompletedObject
{
    public string objectName;
    public ObjectType objectType;
    public ChapterType chapterType;
    public Vector3 position;
    public string completedTime;

    /// <summary>
    /// Get detailed info string
    /// </summary>
    public string GetInfo()
    {
        return $"{objectName} ({objectType}, {chapterType}) - {completedTime}";
    }
}