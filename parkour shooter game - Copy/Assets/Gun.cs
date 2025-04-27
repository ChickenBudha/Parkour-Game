using UnityEngine;
using TMPro;

public class Gun : MonoBehaviour
{
    public int damage;
    public float timeBetweenShots, spread, reloadTime, range, timeBetweenShooting;
    public int magazineSize, bulletsPerTap;
    public bool allowButtonHold;
    public float camShakeMagnitude, camShakeDirection;
    int bulletsLeft, bulletsShot;

    bool shooting, reloading, readyToShoot;
    RaycastHit rayHit;
    public LayerMask whatIsEnemy;

    public GameObject muzzleFlash, bulletHole;
    public Transform attackPoint;
    public Camera cam;
    public RaycastHit hit;
    public CamShake camShake; 
    public TextMeshProUGUI text;

   
    void Start()
    {
        bulletsLeft = magazineSize;
        readyToShoot = true;
    }

    void Update()
    {
        //Set Text
        text.SetText(bulletsLeft + "/" + magazineSize);

        //Input
        if (allowButtonHold)
        {
            shooting = Input.GetKey(KeyCode.Mouse0);
        }
        else shooting = Input.GetKeyDown(KeyCode.Mouse0);

        //Reload
        if (Input.GetKeyDown(KeyCode.R) && !reloading && bulletsLeft < magazineSize)
        {
            reloading = true;
            Invoke("ResetReload", reloadTime);
        }

        //Shoot
        if (shooting && !reloading && bulletsLeft > 0 && readyToShoot)
        {   
            bulletsShot = bulletsPerTap;
            Shoot();

        }
    }

    void Shoot()
    {
            //Spread
            float x = Random.Range(-spread, spread);
            float y = Random.Range(-spread, spread);
            Vector3 direction = cam.transform.forward + new Vector3 (x, y, 0);

            GameObject flash = Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity, attackPoint);
            Destroy(flash, 0.1f);

            if (Physics.Raycast(cam.transform.position, direction, out rayHit, range))
            {
                if (((1 << rayHit.collider.gameObject.layer) & whatIsEnemy) != 0)
                {
                    Debug.Log(rayHit.collider.name);

                    // if (rayHit.collider.CompareTag("Enemy"))
                    // rayHit.collider.GetComponent<ShootingAi>().TakeDamage(damage);
                }
                else
                {
                    Instantiate(bulletHole, rayHit.point, Quaternion.LookRotation(rayHit.normal));
                }
            }

            camShake.Shake(camShakeDirection, camShakeMagnitude);

            readyToShoot = false;
            bulletsLeft--;
            bulletsShot--;
            
            if (bulletsShot > 0 && bulletsLeft > 0) Invoke("Shoot", timeBetweenShots);

            Invoke("ResetShot", timeBetweenShooting);
    }

    void ResetShot()
    {
        readyToShoot = true;
    }

    void ResetReload()
    {
        bulletsLeft = magazineSize;
        reloading = false;
    }
}
