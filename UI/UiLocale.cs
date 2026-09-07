using FireSaveRepair.Core;

namespace FireSaveRepair.UI;

// The repair engine's stable diagnostic strings stay independent of UI state.
// Translate them at presentation time so existing scan results switch language
// immediately, without rescanning files or changing a worker's culture.
public sealed class UiLocale
{
    public bool English { get; set; } = true;
    public string Text(string en,string ru) => English?en:ru;
    static readonly (string Ru,string En)[] CoreMessages = [
        ("Огонь · можно исправить","Fire · ready to repair"),
        ("Огонь · правка заблокирована","Fire · repair blocked"),
        ("Эффект не найден","No fire effect"),
        ("Не удалось проверить","Could not check"),
        ("Исправлен · проверьте в игре","Repaired · test in game"),
        ("FireBreathDamage нет, но есть другой или остаточный урон. Автоисправление запрещено.","No FireBreathDamage, but other or residual damage remains. Automatic repair is blocked."),
        ("FireBreathDamage не найден. Это не проверка всех возможных причин потери здоровья.","FireBreathDamage was not found. Other causes of health loss are not covered."),
        ("Огненный эффект найден несколько раз; этот случай не проверен.","Multiple fire effects were found. This case is not supported."),
        ("Огонь найден, но есть неизвестные эффекты: ","Fire found alongside unknown effects: "),
        (". Запись запрещена.",". Repair is blocked."),
        ("Огонь и другие источники урона: ","Fire and other damage sources: "),
        (". Безопасное разделение не подтверждено.",". They cannot yet be safely separated."),
        ("Огонь найден, но его параметры отличаются от проверенного постоянного эффекта.","Fire found, but its parameters differ from the supported permanent effect."),
        ("Огонь найден, но сохранённые суммы урона не соответствуют проверенному случаю. Запись запрещена.","Fire found, but the cached damage values do not match the supported case. Repair is blocked."),
        ("Найден FireBreathDamage и совпадающий остаточный урон (","FireBreathDamage and matching cached damage found ("),
        ("). Можно подготовить исправление.","). This save is eligible for repair."),
        ("Не найдена таблица FUnitModel.","The FUnitModel table was not found."),
        ("Состояние игрока не распознано. Формат или набор полей не поддерживается.","Player state was not recognized. The format or field layout is unsupported."),
        ("Найдено несколько похожих состояний игрока. Изменения запрещены.","Multiple possible player states were found. Changes are blocked."),
        ("Неверная запись эффекта.","Invalid effect record."),
        ("Неизвестный индекс / источник эффекта.","Unknown effect index or source."),
        ("Неизвестный SID.","Unknown effect identifier."),
        ("Неизвестная карта агрегатов.","Unknown aggregate map."),
        ("Некорректный агрегат.","Invalid aggregate."),
        ("Неизвестные поля перед картой источников.","Unknown fields before the source map."),
        ("Неизвестная карта источников.","Unknown source map."),
        ("Некорректная карта источников.","Invalid source map."),
        ("Некорректный источник.","Invalid source."),
        ("Карта источников вышла за границу данных.","The source map extends beyond the data boundary."),
        ("Непредусмотренное изменение данных.","An unexpected data change was detected."),
        ("Итоговое состояние не прошло проверку.","The repaired state failed validation."),
        ("Изменились данные вне состояния эффектов.","Data outside the effect state was changed."),
        ("Изменились словари.","The name dictionaries were changed."),
        ("Оборванная структура сохранения.","The save structure is truncated."),
        ("Некорректное числовое значение.","Invalid numeric value."),
        ("Неподдерживаемый размер SAV.","Unsupported SAV file size."),
        ("CRC32 не совпадает. Файл повреждён или имеет другой формат; изменения запрещены.","CRC32 mismatch: the file is damaged or uses another format. Changes are blocked."),
        ("Неподдерживаемый распакованный размер.","Unsupported decompressed size."),
        ("Превышен лимит размера.","The size limit was exceeded."),
        ("Неподдерживаемый размер данных.","Unsupported data size."),
        ("Поддерживается только проверенный формат 182 (игра 2.0.4).","Only the verified save format 182 (game 2.0.4) is supported."),
        ("Некорректный указатель словарей.","Invalid dictionary offset."),
        ("Неизвестная структура словарей.","Unknown dictionary structure."),
        ("Неверная кодировка словаря.","Invalid dictionary encoding."),
        ("Неизвестная структура словаря Player / лишние данные.","Unknown Player dictionary structure or unexpected trailing data."),
        ("CampaignsSave.sav — это список кампаний, не сейв игрока.","CampaignsSave.sav is a campaign index, not a player save."),
        ("Выберите файл .sav.","Select a .sav file."),
        ("Сначала выберите распознанный огненный сейв.","First select a supported save with fire damage."),
        ("Сейв изменился после сканирования. Повторите проверку.","The save changed after scanning. Scan it again."),
        ("Закройте S.T.A.L.K.E.R. 2 перед исправлением. Программа не закрывает игру сама.","Close S.T.A.L.K.E.R. 2 before repairing. This app will not close the game for you."),
        ("Папка не найдена.","Folder not found."),
        ("В папке больше 500 сейвов. Выберите более узкую папку.","There are more than 500 saves. Select a smaller folder."),
        ("Проверка записанного файла не прошла: ","Written file verification failed: "),
        ("Исходный сейв изменился. Запись отменена.","The original save changed. Writing was cancelled."),
        ("Сейв изменился. Замена отменена: ","The save changed. Replacement was cancelled: "),
        ("Сейв изменился перед заменой. Оригинал не заменён; временная копия: ","The save changed before replacement. The original was not replaced; temporary file: "),
        ("После замены хеш отличается. Проверьте синхронизацию. Резервная копия: ","The hash differs after replacement. Check cloud synchronization. Backup: "),
        ("Папка отчёта: ","Report folder: "),
        ("Резервная копия: ","Backup: "),
        ("не создавалась","not created"),
        ("Неполная сборка приложения. Используйте готовый полный EXE; скачивание компонентов программой не предусмотрено.","This application build is incomplete. Use the full EXE; components are not downloaded by the app."),
        ("Повреждён встроенный компонент. Проверка сохранений остановлена.","An embedded component is damaged. Save checking was stopped."),
        ("Нужна oo2core_9_win64.dll из доверенного источника (x64, Oodle 9).","A trusted x64 version 9 native runtime is required for this developer build."),
        ("DLL не найдена.","Native component not found."),
        ("Не удалось загрузить DLL (Windows ","The native component could not be loaded (Windows "),
        ("). Нужна версия x64.","). The x64 version is required."),
        ("В DLL нет нужных функций Oodle.","The native component does not provide the required functions."),
        ("Oodle закрыт.","The codec has been closed."),
        ("Не удалось распаковать сохранение; файл не изменён.","The save could not be decompressed; the file was not changed."),
        ("Неверный размер данных для Oodle.","Invalid data size for compression."),
        ("Ошибка сжатия Oodle.","Compression failed."),
        ("Проверка повторной распаковкой не прошла; запись запрещена.","Decompression verification failed; writing is blocked.")
    ];
    static readonly (string Ru,string En)[] Ordered=CoreMessages.OrderByDescending(p=>p.Ru.Length).ToArray();
    public string Diagnostic(string message)
    {
        if(!English)return message;
        foreach(var (ru,en) in Ordered)message=message.Replace(ru,en,StringComparison.Ordinal);
        message=System.Text.RegularExpressions.Regex.Replace(message,@"(matching cached damage found \()(-?\d+),(\d+)(\))",m=>m.Groups[1].Value+m.Groups[2].Value+"."+m.Groups[3].Value+m.Groups[4].Value);
        return message;
    }
    public string Error(Exception e) => e switch {
        RepairException => Diagnostic(e.Message),
        UnauthorizedAccessException => Text("Access denied. Check folder permissions and close other apps using the file.","Нет доступа. Проверьте права на папку и закройте приложения, использующие файл."),
        IOException => Text($"File operation failed (0x{e.HResult:X8}). Check the file, available space and cloud synchronization.",$"Ошибка работы с файлом (0x{e.HResult:X8}). Проверьте файл, свободное место и облачную синхронизацию."),
        _ => Text($"Operation failed ({e.GetType().Name}, 0x{e.HResult:X8}). No further changes will be attempted.",$"Операция остановлена ({e.GetType().Name}, 0x{e.HResult:X8}). Дальнейшие изменения отменены.")
    };
}
