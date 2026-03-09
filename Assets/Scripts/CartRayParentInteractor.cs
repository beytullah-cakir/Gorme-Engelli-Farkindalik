using UnityEngine;
using UnityEngine.InputSystem;

public class CartRayParentInteractor : MonoBehaviour
{
    public InputActionProperty triggerAction;
    public float rayDistance = 5f;
    public Transform xrOrigin;

    private CartParentHold currentCart;

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            CartParentHold cart = hit.collider.GetComponent<CartParentHold>();

            if (cart != null && triggerAction.action.IsPressed())
            {
                if (currentCart == null)
                {
                    currentCart = cart;
                    //currentCart.AttachToXR(xrOrigin);
                }
            }
        }

        if (!triggerAction.action.IsPressed() && currentCart != null)
        {
            //currentCart.Detach();
            currentCart = null;
        }
    }
}