using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Amazon;
using Amazon.S3;
using Amazon.CognitoIdentity;
using System;
using System.Threading.Tasks;
using Amazon.S3.Model;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.UI;
using Amazon.S3.Transfer;

public class S3Manager : MonoBehaviour
{
    private IAmazonS3 s3Client;
    private TransferUtility transferUtil;
    private string folderPath;
    private string _bucketName = "s3kshtest";

    private void Awake()
    {
        folderPath = Application.persistentDataPath;
    }

    private void Start()
    {
        s3Client = new AmazonS3Client(new CognitoAWSCredentials("poolId", RegionEndpoint.APNortheast2), RegionEndpoint.APNortheast2);
        transferUtil = new TransferUtility(s3Client);
        Debug.Log(s3Client);
        StartCoroutine(UnityWebRequestGet());
    }

    public void Button()
    {
        FindFileInDirectory(folderPath);

        //UploadFullDirectoryAsync(transferUtil, _bucketName, "sampleUserID", folderPath);

        Debug.Log("complet");
    }

    public void DeleteButton()
    {
        DeleteObjectNonVersionedBucketAsync(s3Client, _bucketName, "sampleUserID/");
    }

    // 경로 내 파일 s3에 업로드.
    public async void UploadFileAsync(
        IAmazonS3 client,
        string bucketName,
        string objectName,
        string filePath
        )
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectName,
            FilePath = filePath,
            CannedACL = S3CannedACL.PublicRead,
        };

        var response = await client.PutObjectAsync(request);
        Debug.Log(response);
        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            Debug.Log($"Successfully uploaded {objectName} to {bucketName}.");
            return;
        }
        else
        {
            Debug.Log($"Could not upload {objectName} to {bucketName}.");
            return;
        }
    }

    // 폴더 내 파일들 s3에 폴더에 묶어서 저장.
    public async void UploadFullDirectoryAsync(
            TransferUtility transferUtil,
            string bucketName,
            string saveName,
            string localPath)
    {
        if (Directory.Exists(localPath))
        {
            try
            {
                await transferUtil.UploadDirectoryAsync(new TransferUtilityUploadDirectoryRequest
                {
                    BucketName = bucketName,
                    KeyPrefix = saveName,
                    Directory = localPath,
                    CannedACL = S3CannedACL.PublicRead
                });
                Debug.Log("Folder Upload");
                return;
            }
            catch (AmazonS3Exception s3Ex)
            {
                Debug.Log($"Can't upload the contents of {localPath} because:");
                Debug.Log(s3Ex?.Message);
                return;
            }
        }
        else
        {
            Debug.Log($"The directory {localPath} does not exist.");
            return;
        }
    }

    // 파일 삭제
    public async void DeleteObjectNonVersionedBucketAsync(IAmazonS3 client, string bucketName, string keyName)
    {
        try
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = keyName,
            };

            Debug.Log($"Deleting object: {keyName}");
            await client.DeleteObjectAsync(deleteObjectRequest);
            Debug.Log($"Object: {keyName} deleted from {bucketName}.");
        }
        catch (AmazonS3Exception ex)
        {
            Debug.Log($"Error encountered on server. Message:'{ex.Message}' when deleting an object.");
        }
    }


    public void FindFileInDirectory(string folderPath)
    {
        DirectoryInfo di = new DirectoryInfo(folderPath);

        foreach (FileInfo file in di.GetFiles())
        {
            UploadFileAsync(s3Client, _bucketName, file.Name, file.FullName);
        }
    }

    public void FindJsonFileInDirectory(string folderPath)
    {
        DirectoryInfo di = new DirectoryInfo(folderPath);

        foreach (FileInfo file in di.GetFiles("*.json"))
        {
            UploadFileAsync(s3Client, _bucketName, file.Name, file.FullName);
            File.Delete(file.FullName);
        }
    }

    IEnumerator UnityWebRequestGet()
    {
        string url = "https://s3kshtest.s3.ap-northeast-2.amazonaws.com/saveJson";

        UnityWebRequest uwr = UnityWebRequest.Get(url);

        yield return uwr.SendWebRequest();

        if (uwr.error == null)
        {
            Debug.Log(uwr.downloadHandler.text);
        }
        else
        {
            Debug.Log("Error");
        }
    }

}