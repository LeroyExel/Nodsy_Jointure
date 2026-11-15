namespace Jointure
{
    public interface IInteractable
    {
        void OnPrimary(); //Called when the primary button is pressed
        void OnSecondary(); //Called when the secondary button is pressed
        void OnTrigger(); //Called when the trigger is pressed
    }
}
