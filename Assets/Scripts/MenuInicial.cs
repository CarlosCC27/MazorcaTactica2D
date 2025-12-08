using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuInicial : MonoBehaviour
{
    public void Jugar (){
        SceneManager.LoadScene("Escena");
    }

    public void Salir (){
        Debug.Log("Salir del juego");
        Application.Quit();
    }

}
