using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UserInput : MonoBehaviour
{
    public GameObject slot1;

    private Solitaire solitaire;

    private float timer;

    private float doubleClickTime = 0.3f;

    private int clickCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        solitaire = FindObjectOfType<Solitaire>();
        slot1 = this.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (clickCount == 0)
        {
            timer += Time.deltaTime;
        }

        if (clickCount == 3)
        {
            timer = 0;
            clickCount = 1;
        }
        if (timer > doubleClickTime)
        {
            timer = 0;
            clickCount = 0;
        }
        GetMouseClick();
    }

    void GetMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            clickCount++;
            Vector3 mousePosition =
                Camera
                    .main
                    .ScreenToWorldPoint(new Vector3(Input.mousePosition.x,
                        Input.mousePosition.y,
                        -10));
            RaycastHit2D hit =
                Physics2D
                    .Raycast(Camera
                        .main
                        .ScreenToWorldPoint(Input.mousePosition),
                    Vector2.zero);

            if (hit)
            {
                if (hit.collider.CompareTag("Deck"))
                {
                    Deck();
                }
                else if (hit.collider.CompareTag("Card"))
                {
                    Card(hit.collider.gameObject);
                }
                else if (hit.collider.CompareTag("Top"))
                {
                    Top(hit.collider.gameObject);
                }
                else if (hit.collider.CompareTag("Bottom"))
                {
                    Bottom(hit.collider.gameObject);
                }
            }
        }
    }

    void Deck()
    {
        print("Clicked deck");
        solitaire.DealFromDeck();
        slot1 = this.gameObject;
    }

    void Card(GameObject selected)
    {
        print("Clicked card");

        if (!selected.GetComponent<Selectable>().faceUp)
        {
            if (!Blocked(selected))
            {
                selected.GetComponent<Selectable>().faceUp = true;
                slot1 = this.gameObject;
            }
        }
        else if (selected.GetComponent<Selectable>().inDeckPile)
        {
            if (!Blocked(selected))
            {
                if (slot1 == selected)
                {
                    if (DoubleClick())
                    {
                        // try auto stack
                        AutoStack (selected);
                    }
                }
                else
                {
                    slot1 = selected;
                }
            }
        }

        // check here for top 3 select logic
        if (slot1 == this.gameObject)
        {
            slot1 = selected;
        }
        else if (slot1 != selected)
        {
            if (Stackable(selected))
            {
                Stack (selected);
            }
            else
            {
                slot1 = selected;
            }
        }
        else if (slot1 == selected)
        {
            if (DoubleClick())
            {
                // attempt to auto stack
                AutoStack (selected);
            }
        }
    }

    void Top(GameObject selected)
    {
        print("CLicked top");
        if (slot1.CompareTag("Card"))
        {
            // stack aces on empty
            if (slot1.GetComponent<Selectable>().value == 1)
            {
                Stack (selected);
            }
        }
    }

    void Bottom(GameObject selected)
    {
        print("CLicked bottom");

        // Kings can stack on the empty bottom row spaces
        if (slot1.CompareTag("Card"))
        {
            if (slot1.GetComponent<Selectable>().value == 13)
            {
                Stack (selected);
            }
        }
    }

    bool Stackable(GameObject selected)
    {
        Selectable s1 = slot1.GetComponent<Selectable>();
        Selectable s2 = selected.GetComponent<Selectable>();

        if (s2.inDeckPile)
        {
            return false;
        }
        if (s2.top)
        {
            if (s1.suit == s2.suit || (s1.value == 1 && s2.suit == null))
            {
                if (s1.value == s2.value + 1)
                {
                    return true;
                }
            }

            return false;
        }
        else
        {
            if (s1.value == s2.value - 1)
            {
                bool card1Red = true;
                bool card2Red = true;
                if (s1.suit == "C" || s1.suit == "S")
                {
                    card1Red = false;
                }
                if (s2.suit == "C" || s2.suit == "S")
                {
                    card2Red = false;
                }
                if (card1Red == card2Red)
                {
                    return false;
                }
                return true;
            }
        }
        return false;
    }

    void Stack(GameObject selected)
    {
        Selectable s1 = slot1.GetComponent<Selectable>();
        Selectable s2 = selected.GetComponent<Selectable>();
        float yOffset = 0.3f;

        if (s2.top || (!s1.top && s1.value == 13))
        {
            yOffset = 0;
        }

        slot1.transform.position =
            new Vector3(selected.transform.position.x,
                selected.transform.position.y - yOffset,
                selected.transform.position.z - 0.01f);

        slot1.transform.parent = selected.transform; // move children with parents

        if (s1.inDeckPile)
        {
            solitaire.tripsOnDisplay.Remove(slot1.name);
        }
        else if (s1.top && s2.top && s1.value == 1)
        {
            solitaire.topPos[s1.row].GetComponent<Selectable>().value = 0;
            solitaire.topPos[s1.row].GetComponent<Selectable>().suit = null;
        }
        else if (s1.top)
        {
            solitaire.topPos[s1.row].GetComponent<Selectable>().value =
                s1.value - 1;
        }
        else
        {
            solitaire.bottoms[s1.row].Remove(slot1.name);
        }

        s1.inDeckPile = false;
        s1.row = s2.row;

        if (s2.top)
        {
            solitaire.topPos[s1.row].GetComponent<Selectable>().value =
                s1.value;
            solitaire.topPos[s1.row].GetComponent<Selectable>().suit = s1.suit;
            s1.top = true;
        }
        else
        {
            s1.top = false;
        }

        slot1 = this.gameObject;
    }

    bool Blocked(GameObject selected)
    {
        Selectable s2 = selected.GetComponent<Selectable>();
        if (s2.inDeckPile)
        {
            if (s2.name == solitaire.tripsOnDisplay.Last())
            {
                return false;
            }
            print(s2.name +
            " is blocked by " +
            solitaire.tripsOnDisplay.Last());
            return true;
        }
        else
        {
            if (s2.name == solitaire.bottoms[s2.row].Last())
            {
                return false;
            }

            return true;
        }
    }

    bool DoubleClick()
    {
        if (timer < doubleClickTime && clickCount == 2)
        {
            print("Double Click");
            return true;
        }

        return false;
    }

    void AutoStack(GameObject selected)
    {
        for (int i = 0; i < solitaire.topPos.Length; i++)
        {
            // using stack as both noun and verb 🤕
            Selectable stack = solitaire.topPos[i].GetComponent<Selectable>();
            if (
                selected.GetComponent<Selectable>().value == 1 // ace
            )
            {
                if (solitaire.topPos[i].GetComponent<Selectable>().value == 0)
                {
                    slot1 = selected;
                    Stack(stack.gameObject);
                    break;
                }
            }
            else
            {
                if (
                    (
                    solitaire.topPos[i].GetComponent<Selectable>().suit ==
                    slot1.GetComponent<Selectable>().suit
                    ) &&
                    (
                    solitaire.topPos[i].GetComponent<Selectable>().value ==
                    slot1.GetComponent<Selectable>().value - 1
                    )
                )
                {
                    if (HasNoChildren(slot1))
                    {
                        slot1 = selected;
                        string lastCardName =
                            stack.suit + stack.value.ToString();
                        if (stack.value == 1)
                        {
                            lastCardName = stack.suit + "A";
                        }
                        else if (stack.value == 11)
                        {
                            lastCardName = stack.suit + "J";
                        }
                        else if (stack.value == 12)
                        {
                            lastCardName = stack.suit + "Q";
                        }
                        else if (stack.value == 13)
                        {
                            lastCardName = stack.suit + "K";
                        }

                        // exprensive but useful
                        GameObject lastCard = GameObject.Find(lastCardName);
                        Stack (lastCard);
                        break;
                    }
                }
            }
        }
    }

    bool HasNoChildren(GameObject card)
    {
        int i = 0;
        foreach (Transform child in card.transform)
        {
            i++;
        }

        return i == 0 ? true : false;
    }
}
