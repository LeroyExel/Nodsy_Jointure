namespace Jointure
{
    public interface IGrabbable
    {
        void OnGrab(); //Called when the object is grabbed
        void OnRelease(); //Called when the object is released
    }
}