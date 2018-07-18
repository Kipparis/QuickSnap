using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetCamera : MonoBehaviour {
    static public TargetCamera S;

    public bool editMode = true;
    public Camera fpCamera; // Камера от первого лица

    // Максимальное отклонение в Shot.position
    public float maxPosDeviation = 1f;
    // Максимальное отклонение в Shot.target
    public float maxTarDeviation = 0.5f;
    // Смягчение для этих отклонений
    public string deviationEasing = Easing.Out;
    public float passingAccuracy = 0.7f;

    public bool checkToDeletePlayerPrefs = false;

    public bool ____________;

    public Rect camRectNormal;  // Выбирается из camera.rect

    public int shotNum;
    public Text shotCounter, shotRating;
    public RawImage checkMark;

    public Shot lastShot;
    public int numShots;
    public Shot[] playerShots;
    public float[] playerRatings;

    void Awake() {
        S = this;
    }

    void Start() {
        // Находим UI элементы
        GameObject go = GameObject.Find("ShotCounter");
        shotCounter = go.GetComponent<Text>();

        go = GameObject.Find("ShotRating");
        shotRating = go.GetComponent<Text>();

        go = GameObject.Find("_Check_64");
        checkMark = go.GetComponent<RawImage>();
        // Прячем марку
        checkMark.enabled = false;

        // Загружаем все снимки из PlayerPrefs
        Shot.LoadShots();
        // Если снимки были
        if(Shot.shots.Count > 0) {
            shotNum = 0;
            ResetPlayerShotsAndRatings();
            ShowShot(Shot.shots[shotNum]);
        }

        // Прячем курсор ( Заметка: не работает в Unity Editor, пока игра не максимизированна )
        Cursor.visible = false;

        camRectNormal = GetComponent<Camera>().rect;
    }

    void ResetPlayerShotsAndRatings() {
        numShots = Shot.shots.Count;
        // Создаём playerShots и playerRatings с базовыми значениями
        playerShots = new Shot[numShots];
        playerRatings = new float[numShots];
    }

    void Update() {
        Shot sh;

        // Ввод с мышки
        // ЛКМ или ПКМ
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) {
            sh = new Shot();
            // Берём позицию и поворот камеры первого лица
            sh.position = fpCamera.transform.position;
            sh.rotation = fpCamera.transform.rotation;
            // Выстреливаем лучом из камеры и смотрим во что он ударится
            Ray ray = new Ray(sh.position, fpCamera.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                sh.target = hit.point;
            }

            if (editMode) {
                if (Input.GetMouseButtonDown(0)) {
                    // ЛКМ делает новый снимок
                    Shot.shots.Add(sh);
                    shotNum = Shot.shots.Count - 1;
                } else if (Input.GetMouseButtonDown(1)) {
                    // ПКМ заменяет текущий снимок
                    Shot.ReplaceShot(shotNum, sh);
                    ShowShot(Shot.shots[shotNum]);
                }
                // Обнуляем инфу о игроке в редактирующем моде
                ResetPlayerShotsAndRatings();
            } else {
                // Сверяем этот снимок с текущим выстрелом
                float acc = Shot.Compare(Shot.shots[shotNum], sh);
                lastShot = sh;
                playerShots[shotNum] = sh;
                playerRatings[shotNum] = acc;
            }

            // Ставим _TargetCamera для показания Shot
            // ShowShot(sh);

            Utils.tr(sh.ToXML());

            // Записываем новый снимок
            //Shot.shots.Add(sh);
            //shotNum = Shot.shots.Count - 1;
        }

        // Ввод с клавиатуры
        // Используем Q и E для выбора снимков
        // Заметка: Всё из этого будет призывать ошибку если Shot.shots пуст
        if (Input.GetKeyDown(KeyCode.Q)) {  // ShowShow(Shot.shots[(shotNum--)%Shot.shots.Count])
            shotNum--;
            if (shotNum < 0) shotNum = Shot.shots.Count - 1;
            ShowShot(Shot.shots[shotNum]);
        }
        if (Input.GetKeyDown(KeyCode.E)) {
            shotNum++;
            if (shotNum >= Shot.shots.Count) shotNum = 0;
            ShowShot(Shot.shots[shotNum]);
        }
        // Если editMode активен и левый шифт нажат...
        if (editMode && Input.GetKey(KeyCode.LeftShift)) {
            // Используем Shift-S чтобы сохранить
            if (Input.GetKeyDown(KeyCode.S)) {
                Shot.SaveShots();
            }
            // Shift-X чтобы вывести XML в консоль
            if (Input.GetKeyDown(KeyCode.X)) {
                Utils.tr(Shot.XML);
            }
        }

        // Удерживаем Tab чтобы увеличить Target окно
        if (Input.GetKeyDown(KeyCode.Tab)) {
            Rect fpRect = GameObject.Find("FirstPersonCharacter").GetComponent<Camera>().rect;
            //= new Rect(fpRect);
            GetComponent<Camera>().rect = new Rect(fpRect);
        }
        if (Input.GetKeyUp(KeyCode.Tab)) {
            // Возвращаем нормальный размер
            GetComponent<Camera>().rect = camRectNormal;
        }

        // Обновляем UI text
        shotCounter.text = (shotNum + 1).ToString() + " of " + Shot.shots.Count;
        if (Shot.shots.Count == 0) shotCounter.text = "No shots exist";
        //shotRating.text = "";

        if (playerRatings.Length > shotNum && playerRatings[shotNum] != null) {
            float rating = Mathf.Max(Mathf.Round(playerRatings[shotNum] * 100f), 0);
            shotRating.text = rating.ToString() + "%";
            checkMark.enabled = (playerRatings[shotNum] > passingAccuracy);
        } else {
            shotRating.text = "";
            checkMark.enabled = false;
        }
    }

    public void ShowShot(Shot sh) {
        // Ставим _TargetCamera
        transform.position = sh.position;
        transform.rotation = sh.rotation;
    }

    // OnDrawGizmos() вызывается в любое время когда Gizmos должны быть нарисованны,
    // Даше если Unity не играет
    public void OnDrawGizmos() {
        List<Shot> shots = Shot.shots;
        for (int i = 0; i < shots.Count; i++) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(shots[i].position, 0.5f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(shots[i].position, shots[i].target);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(shots[i].target, 0.25f);
        }

        // Если  checkToDeletePlayerPrefs выбранно
        if (checkToDeletePlayerPrefs) {
            Shot.DeleteShots(); // Удаляем все выстрелы
            // Убираем галочку
            checkToDeletePlayerPrefs = false;
            shotNum = 0;    // Текущий выстрел = 0
        }

        // Показываем последнюю попытку игрока снимка
        if (lastShot != null) {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(lastShot.position, 0.25f);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(lastShot.position, lastShot.target);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastShot.target, 0.125f);
        }
    }
}
