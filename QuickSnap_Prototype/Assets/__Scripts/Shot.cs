using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]   // Делаем видимым в инспекторе
public class Shot { // Не наследует MonoBehaviour

    static public List<Shot> shots = new List<Shot>();  // Список все снимков
    static public string prefsName = "QuickSnap_Shots";

    public Vector3 position;    // Позиция камеры
    public Quaternion rotation; // Поворот камеры
    public Vector3 target;  // Куда камера показывает

    // Создаёт запись в одну линию для XML документа
    public string ToXML() {
        string ss = "<shot ";
        ss += "x=\"" + position.x + "\" ";
        ss += "y=\"" + position.y + "\" ";
        ss += "z=\"" + position.z + "\" ";
        ss += "qx=\"" + rotation.x + "\" ";
        ss += "qy=\"" + rotation.y + "\" ";
        ss += "qz=\"" + rotation.z + "\" ";
        ss += "qw=\"" + rotation.w + "\" ";
        ss += "tx=\"" + target.x + "\" ";
        ss += "ty=\"" + target.y + "\" ";
        ss += "tz=\"" + target.z + "\" ";
        ss += " />";

        return (ss);
    }

    // Берёт PT_XMLHashtable запись снимка крч, и переводит её в объект класса Shot
    static public Shot ParseShotXML(PT_XMLHashtable xHT) {
        Shot sh = new Shot();

        sh.position.x = float.Parse(xHT.att("x"));
        sh.position.y = float.Parse(xHT.att("y"));
        sh.position.z = float.Parse(xHT.att("z"));
        sh.rotation.x = float.Parse(xHT.att("qx"));
        sh.rotation.y = float.Parse(xHT.att("qy"));
        sh.rotation.z = float.Parse(xHT.att("qz"));
        sh.rotation.w = float.Parse(xHT.att("qw"));
        sh.target.x = float.Parse(xHT.att("tx"));
        sh.target.y = float.Parse(xHT.att("ty"));
        sh.target.z = float.Parse(xHT.att("tz"));

        return (sh);
    }

    // Загружает все выстрелы из PlayerPrefs
    static public void LoadShots() {
        // Пустой список снимков
        shots = new List<Shot>();
        if (!PlayerPrefs.HasKey(prefsName)) {
            // Пока ещё нет снимков
            return;
        }

        // Полностью достаём XML и переводим его
        string shotsXML = PlayerPrefs.GetString(prefsName);
        PT_XMLReader xmlr = new PT_XMLReader();
        xmlr.Parse(shotsXML);

        // Достаём список всех <shot>s
        PT_XMLHashList hl = xmlr.xml["xml"][0]["shot"];
        for (int i = 0; i < hl.Count; i++) {
            // Делаем сокращение
            PT_XMLHashtable ht = hl[i];
            Shot sh = ParseShotXML(ht);
            // Добавляем в список
            shots.Add(sh);
        }
    }

    // Сохраняем список <Shot>  в PlayerPrefs
    static public void SaveShots() {
        string xs = Shot.XML;

        Utils.tr(xs);   // расписываем весь XML в консольку

        // Задаём PlayerPrefs
        PlayerPrefs.SetString(prefsName, xs);

        Utils.tr("PleyerPrefs." + prefsName + " has been set.");
    }

    // Конвертируем все выстрелы в XML
    static public string XML {
        get {
            // Начинаем XML строку
            string xs = "<xml> \n";
            // Добавляем каждый выстрел как <shot> в XML
            foreach (Shot sh in Shot.shots) {
                xs += sh.ToXML() + "\n";
            }
            // Добавляем закрывающий таг
            xs += "</xml>";
            return (xs);
        }
    }

    // Удаляем снимки из списка и из PlayerPrefs
    static public void DeleteShots() {
        shots = new List<Shot>();
        if (PlayerPrefs.HasKey(prefsName)) {
            PlayerPrefs.DeleteKey(prefsName);
            Utils.tr("PlayerPrefs." + prefsName + " has been deleted.");
        } else {
            Utils.tr("There was no PlayerPrefs." + prefsName + " to delete");
        }
    }

    // Заменяем снимок
    static public void ReplaceShot(int ndx, Shot sh) {
        // Убеждаемся что действительно есть такой снимок
        if (shots == null || shots.Count <= ndx) return;
        // Убираем старый выстрел
        shots.RemoveAt(ndx);
        // List<>.Insert() добавляет что то в лист в определённый индекс
        shots.Insert(ndx, sh);

        Utils.tr("Replaced shot:", ndx, "with", sh.ToXML());
    }

    // Сравниваем два снимка. 1 - идеально подходит, <0 не подходит
    public static float Compare(Shot target, Shot test) {
        // Считаем отклонение камеры и удара от Raycast
        float posDev = (test.position - target.position).magnitude;
        float tarDev = (test.target - target.target).magnitude;

        float posAccPct, tarAccPct, posAP2, tarAP2; // Степень точности

        TargetCamera tc = TargetCamera.S;

        // Получаем значение точности, где 1 - идеально, 0 - кое как подходит
        posAccPct = 1 - (posDev / tc.maxPosDeviation);
        tarAccPct = 1 - (tarDev / tc.maxTarDeviation);

        // Смягчаем значения чтобы оно было более прощающее
        posAP2 = Easing.Ease(posAccPct, tc.deviationEasing);
        tarAP2 = Easing.Ease(tarAccPct, tc.deviationEasing);

        float accuracy = (posAP2 + tarAP2) / 2f;

        // Используем Utils чтобы оформить числа в хорошенькую строку
        string accText = Utils.RoundToPlaces(accuracy * 100).ToString() + "%";
        Utils.tr("Position:", posAccPct, posAP2, "Target:", tarAccPct, tarAP2, "Accuracy", accuracy);

        return (accuracy);
    }
}
