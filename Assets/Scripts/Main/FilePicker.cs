using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FilePicker : MonoBehaviour
{
    // Start is called before the first frame update
	//private string pngFileType;

	string picked_file_path;

	void Start()
	{
		//pngFileType = NativeFilePicker.ConvertExtensionToFileType( "png" ); // Returns "application/pdf" on Android and "com.adobe.pdf" on iOS
		//Debug.Log( "png's MIME/UTI is: " + pngFileType );
	}

	public void BrowesImages(){
		
		if( !NativeFilePicker.IsFilePickerBusy() ){
			#if UNITY_ANDROID
				// Use MIMEs on Android
				string[] fileTypes = new string[] { "image/*" };
			#else
				// Use UTIs on iOS
				string[] fileTypes = new string[] { "public.image" };
			#endif
			Debug.Log("file types: " + fileTypes);
			// Pick a PDF file
			NativeFilePicker.Permission permission = NativeFilePicker.PickFile( ( path ) =>
			{
				if( path == null )
					Debug.Log( "Operation cancelled" );
				else
					Debug.Log( "Picked file: " + path );
					picked_file_path=path;
					gameObject.GetComponent<ProfileController>().DisplayNewPic(path);
			},  fileTypes );

			Debug.Log( "Permission result: " + permission );
		}
					
	}

	public string get_file_path(){
		return picked_file_path;
	}
	void Update()
	{
		
		
	}
}
