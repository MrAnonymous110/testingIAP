using UnityEngine;
using System.Collections;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T mInstance;
 
    public static T Instance
    {
        get
        {
	        if ( null == mInstance )
	        {
		        mInstance = ( T ) FindObjectOfType( typeof(T) );

		        // if ( null == mInstance )
		        // {
          //           TFUtils.LogWarning("Singleton object '" + typeof(T).ToString() + "' not found. Does it need to be added to PersistentManagers?");
		        // }
	        }
 
	        return mInstance;
        }
    }

    public static bool HasInstance
    {
    	get
    	{
    		return mInstance != null;
    	}
    }

    public void Destroy()
    {
        if(mInstance == this)
            mInstance = null;
    }

    protected virtual void Awake() {
        if(mInstance == null)
            mInstance = this as T;
    }

    protected virtual void OnDestroy()
    {
        Destroy();
    }
}