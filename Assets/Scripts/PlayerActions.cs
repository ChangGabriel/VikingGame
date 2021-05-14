using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class PlayerActions : MonoBehaviour
{
    // Connected objects
    private PlayerClass playerClass;
    private Rigidbody2D playerRB;
    private Animator animator;
    public GameObject damageNumbers;
    public Transform shadow;

    // Player stats
    private int level = 1;
    private float maxHealth;
    private float currentHealth;
    public float movespeed;

    // Experience and level related
    private int currentExp = 0;
    private int requiredExp = 10;

    // UI related
    public Canvas canvas;
    private Image HPbar;
    private TMP_Text HPcurrent;
    private TMP_Text HPmax;
    private Image XPbar;
    private TMP_Text levelText;

    // Material related
    private Material matWhenHit;
    private Material matDefault;
    private Material matDeath;

    // Hidden variables in inspector
    [HideInInspector]
    public bool isActive = false;
    [HideInInspector]
    public bool isInvulnerable = false;
    [HideInInspector]
    public bool moveable = true;
    [HideInInspector]
    public Vector2 movement;
    [HideInInspector]
    public SpriteRenderer spriteRenderer;


    // Unity functions
    private void Start() 
    {
        playerClass = GetComponent<PlayerClass>();
        playerRB = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = transform.GetComponent<SpriteRenderer>();
        maxHealth = playerClass.maxHealth;
        currentHealth = maxHealth;
        // Set up Material
        matWhenHit = Resources.Load("Materials/Player-Flash", typeof(Material)) as Material;
        matDeath = Resources.Load("Materials/Dissolve", typeof(Material)) as Material;
        matDefault = spriteRenderer.material;
        // Set up UI
        // HP related UI
        HPbar = canvas.transform.GetChild(1).GetChild(1).GetComponent<Image>();
        HPcurrent = canvas.transform.GetChild(1).GetChild(3).GetComponent<TMP_Text>();
        HPmax = canvas.transform.GetChild(1).GetChild(5).GetComponent<TMP_Text>();
        HPbar.fillAmount = 1;
        HPmax.text = currentHealth.ToString();
        HPcurrent.text = currentHealth.ToString();
        // XP related
        XPbar = canvas.transform.GetChild(2).GetChild(1).GetComponent<Image>();
        levelText = canvas.transform.GetChild(2).GetChild(3).GetComponent<TMP_Text>();
        XPbar.fillAmount = 0f;
        levelText.text = "Lv. " + level;
    }

    private void Update()
    {
        if (moveable)
            playerFaceDirection();
    }

    private void FixedUpdate()
    {
        move();
    }

    // Level and EXP related
    public void GainExp(int exp)
    {
        currentExp += exp;
        XPbar.fillAmount = (float) currentExp / (float) requiredExp;

        if (currentExp >= requiredExp){
            LevelUp();
        }
    }

    private void LevelUp()
    {
        //Modify stats
        level += 1;
        playerClass.increaseDamage(2);
        playerClass.dropUpgrade(level);

        //Modify Exp values
        currentExp -= requiredExp;
        requiredExp += level * 5;

        // Update UI for new level
        levelText.text = "Lv. " + level.ToString();
        XPbar.fillAmount = currentExp / requiredExp;
    }

    public void playerTakeDamage(float damage)
    {
        if (isInvulnerable) return;

        currentHealth -= damage;


        if (currentHealth < 0) 
        {
            currentHealth = 0; //stops UI from displaying negative HP
        }
        HPcurrent.text = currentHealth.ToString();
        HPbar.fillAmount = currentHealth / maxHealth;

        if (currentHealth <= 0)
        {
            isInvulnerable = true;
            moveable = false;
            isActive = true; //stops player from attacking when death effect is playing
            playerRB.velocity = Vector2.zero;
            GetComponent<BoxCollider2D>().enabled = false;
            StartCoroutine(die());
        }
        else 
        {
            StopCoroutine(playerFlash());
            StartCoroutine(playerFlash());
        }
    }

    public void playerRestoreHealth(float amount)
    {
        currentHealth += amount;

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        HPcurrent.text = currentHealth.ToString();
        HPbar.fillAmount = currentHealth / maxHealth;
        TextMesh damageNumber = Instantiate(damageNumbers, transform.position, Quaternion.identity).transform.GetChild(0).GetComponent<TextMesh>();
        damageNumber.color = Color.green;
        damageNumber.text = amount.ToString();
    }

    //Handle player death
    private IEnumerator die()
    {
        spriteRenderer.material = matDeath;
        float ticks = 10f;
        for (int i = 1; i < ticks + 1; i++)
        {
            spriteRenderer.material.SetFloat("_Fade", 1 - i / ticks);
            yield return new WaitForSeconds(0.2f);
        }
        Debug.Log("You Died");
        yield return new WaitForSeconds(3.0f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); //restart to current scene

    }
    //Handles the players facing direction
    void playerFaceDirection()
    {
        movement.x = Input.GetAxisRaw("Horizontal"); //when moving right movement.x = 1 and left movement.x = -1
        movement.y = Input.GetAxisRaw("Vertical");

        //Don't turn when in attack animation, even if you are eg. holding "D" (moving right)
        if (movement.x > 0 && !animator.GetCurrentAnimatorStateInfo(0).IsTag("attack"))
        {
            spriteRenderer.flipX = false;
            shadow.localPosition = new Vector2(-0.02f, 0);
        }
        if (movement.x < 0 && !animator.GetCurrentAnimatorStateInfo(0).IsTag("attack"))
        {
            spriteRenderer.flipX = true;
            shadow.localPosition = new Vector2(0.02f, 0);
        }
    }

    private void move()
    {
        if (moveable && !animator.GetCurrentAnimatorStateInfo(0).IsTag("attack"))
        {
            playerRB.MovePosition(playerRB.position + movement.normalized * movespeed * Time.fixedDeltaTime);
        }
        animator.SetFloat("Speed", (movement.normalized * movespeed * Time.fixedDeltaTime).magnitude * Convert.ToInt32(moveable));
    }

    //handles the player flashing when taking damage
    private IEnumerator playerFlash()
    {
        
        for(int i = 0; i < 3; i++)
        {
            if (currentHealth <= 0)
            {
                yield break; //having this fixes bug where death effect doesen't play if killed by simultaneous attacks
            }
            spriteRenderer.material = matWhenHit;
            yield return new WaitForSeconds(0.1f);
            if (currentHealth <= 0)
            {
                yield break;
            }
            spriteRenderer.material = matDefault;
            yield return new WaitForSeconds(0.1f);
        }
    }
}