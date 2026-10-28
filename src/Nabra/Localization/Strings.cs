using System.Collections.Generic;

namespace Nabra.Localization
{
    /// <summary>All UI text, Arabic and English. Add a language by adding a third column.</summary>
    internal static class Strings
    {
        public readonly struct Pair
        {
            public Pair(string ar, string en)
            {
                Ar = ar;
                En = en;
            }

            public string Ar { get; }
            public string En { get; }
        }

        private static void Add(Dictionary<string, Pair> d, string key, string ar, string en) => d[key] = new Pair(ar, en);

        public static readonly Dictionary<string, Pair> Table = Build();

        private static Dictionary<string, Pair> Build()
        {
            var d = new Dictionary<string, Pair>();

            // ---- navigation / shell
            Add(d, "nav_home", "الرئيسية", "Home");
            Add(d, "nav_shortcuts", "الاختصارات", "Shortcuts");
            Add(d, "nav_settings", "الإعدادات", "Settings");
            Add(d, "nav_about", "حول Nabra", "About");
            Add(d, "side_quick_toggle", "تفعيل / إيقاف سريع", "Quick on / off");
            Add(d, "status_on", "Nabra مفعّل", "Nabra is on");
            Add(d, "status_off", "Nabra متوقف", "Nabra is off");

            // ---- home
            Add(d, "home_title", "الرئيسية", "Home");
            Add(d, "home_subtitle", "أصلِح النص المكتوب بتخطيط لوحة مفاتيح خاطئ بضغطة واحدة، في أي تطبيق.", "Fix text typed with the wrong keyboard layout with one key press, in any app.");
            Add(d, "home_status_hint_on", "حدّد النص في أي تطبيق ثم اضغط الاختصار لتصحيحه.", "Select text in any app, then press the shortcut to fix it.");
            Add(d, "home_status_hint_off", "الاختصار محرَّر الآن ويمكن لتطبيقات أخرى استخدامه.", "The shortcut is released and other apps can use it.");
            Add(d, "home_hotkey_title", "الاختصار الحالي", "Current shortcut");
            Add(d, "home_hotkey_desc", "يعمل في Word وNotepad والمتصفح وغيرها.", "Works in Word, Notepad, your browser and more.");
            Add(d, "btn_change_hotkey", "تغيير الاختصار", "Change shortcut");
            Add(d, "home_mode_title", "وضع التحويل", "Conversion mode");
            Add(d, "mode_auto", "اكتشاف تلقائي", "Auto-detect");
            Add(d, "mode_to_ar", "دائمًا إلى العربية", "Always to Arabic");
            Add(d, "mode_to_en", "دائمًا إلى الإنجليزية", "Always to English");
            Add(d, "mode_auto_desc", "يحدد Nabra الاتجاه حسب أغلب الحروف في النص المحدد.", "Nabra picks the direction from the letters in the selection.");
            Add(d, "mode_to_ar_desc", "يحوّل الحروف الإنجليزية إلى ما يقابلها على لوحة المفاتيح العربية.", "Turns English-layout letters into their Arabic-layout equivalents.");
            Add(d, "mode_to_en_desc", "يحوّل الحروف العربية إلى ما يقابلها على لوحة المفاتيح الإنجليزية.", "Turns Arabic-layout letters into their English-layout equivalents.");
            Add(d, "tester_title", "تجربة تحويل محلية", "Local conversion test");
            Add(d, "tester_desc", "اكتب أو الصق نصًا هنا لترى النتيجة. لا يخرج أي شيء من جهازك.", "Type or paste text to preview the result. Nothing leaves your device.");
            Add(d, "tester_placeholder", "اكتب نصًا في الأعلى، مثل: lvpfh", "Type something above, for example: lvpfh");
            Add(d, "tester_nochange", "لا يوجد ما يحتاج إلى تحويل في هذا النص.", "Nothing to convert in this text.");
            Add(d, "btn_copy", "نسخ النتيجة", "Copy result");
            Add(d, "btn_example", "مثال", "Example");
            Add(d, "btn_clear", "مسح", "Clear");
            Add(d, "dir_en_to_ar", "إنجليزي ← عربي", "English → Arabic");
            Add(d, "dir_ar_to_en", "عربي ← إنجليزي", "Arabic → English");
            Add(d, "last_none", "لم يتم أي تحويل بعد في هذه الجلسة.", "No conversion yet in this session.");
            Add(d, "last_success", "آخر تحويل: {0} — {1} حرفًا — {2}", "Last conversion: {0} — {1} characters — {2}");
            Add(d, "last_failed", "آخر محاولة لم تكتمل — {0}", "Last attempt did not complete — {0}");
            Add(d, "hk_reg_failed_inuse", "تعذّر تسجيل الاختصار الحالي لأنه مستخدم من تطبيق آخر. اختر تركيبة مختلفة.", "The current shortcut could not be registered because another app uses it. Choose a different combination.");
            Add(d, "hk_reg_failed", "تعذّر تسجيل الاختصار الحالي. جرّب تركيبة مختلفة.", "The current shortcut could not be registered. Try a different combination.");

            // ---- capture dialog
            Add(d, "cap_window_title", "اختصار جديد", "New shortcut");
            Add(d, "cap_prompt", "اضغط تركيبة المفاتيح الجديدة الآن", "Press the new key combination now");
            Add(d, "cap_hint", "استخدم Ctrl أو Alt أو Win مع مفتاح، مثل Ctrl + Shift + K. اضغط Esc للإلغاء.", "Use Ctrl, Alt or Win with a key, for example Ctrl + Shift + K. Press Esc to cancel.");
            Add(d, "cap_waiting", "في انتظار الضغط…", "Waiting for keys…");
            Add(d, "btn_save", "حفظ", "Save");
            Add(d, "btn_cancel", "إلغاء", "Cancel");

            // ---- shortcuts page
            Add(d, "sc_subtitle", "اختر الاختصار الذي يناسبك. يُفحص تلقائيًا قبل الحفظ ويعمل فورًا.", "Choose the shortcut that suits you. It is checked before saving and works immediately.");
            Add(d, "sc_convert_title", "اختصار التحويل", "Conversion shortcut");
            Add(d, "sc_convert_desc", "الاختصار الذي يصحّح النص المحدد في أي تطبيق.", "The shortcut that fixes the selected text in any app.");
            Add(d, "btn_restore_default", "استعادة الافتراضي (Ctrl + Alt + K)", "Restore default (Ctrl + Alt + K)");
            Add(d, "sc_quick_title", "اختيارات سريعة", "Quick picks");
            Add(d, "sc_quick_desc", "اضغط على أي تركيبة ليفحصها Nabra ويفعّلها مباشرة.", "Click any combination and Nabra will check and activate it right away.");
            Add(d, "sc_toggle_title", "اختصار التفعيل والإيقاف (اختياري)", "On / off shortcut (optional)");
            Add(d, "sc_toggle_desc", "اختصار ثانٍ يشغّل Nabra أو يوقفه دون فتح النافذة.", "A second shortcut that turns Nabra on or off without opening the window.");
            Add(d, "btn_set_hotkey", "تعيين اختصار", "Set shortcut");
            Add(d, "btn_remove", "إزالة", "Remove");
            Add(d, "sc_not_set", "غير معيّن", "Not set");
            Add(d, "sc_tips_title", "لماذا لا نستخدم Ctrl + K؟", "Why not Ctrl + K?");
            Add(d, "sc_tips_body", "Ctrl + K محجوز في Microsoft Word لإدراج رابط. إن اخترته فسيأخذه Nabra من Word. الأفضل تركيبة تحتوي على Ctrl وAlt معًا، مثل Ctrl + Alt + K. يُنصح بتجنب Alt + Shift لأنه يبدّل لغة لوحة المفاتيح في Windows.", "Ctrl + K is reserved by Microsoft Word for inserting a link. If you choose it, Nabra will take it away from Word. A combination with both Ctrl and Alt, such as Ctrl + Alt + K, is safer. Avoid Alt + Shift: Windows uses it to switch keyboard language.");

            // ---- shortcut validation messages
            Add(d, "hk_ok", "الاختصار متاح ويمكن حفظه.", "This shortcut is available and can be saved.");
            Add(d, "hk_saved_fmt", "تم حفظ الاختصار وتفعيله: {0}", "Shortcut saved and active: {0}");
            Add(d, "hk_err_key", "هذا المفتاح غير مدعوم. استخدم حرفًا أو رقمًا أو مفتاح وظيفة.", "This key is not supported. Use a letter, digit or function key.");
            Add(d, "hk_err_nomod", "يجب أن يتضمن الاختصار مفتاح تعديل واحدًا على الأقل (Ctrl أو Alt أو Win).", "A shortcut needs at least one modifier key (Ctrl, Alt or Win).");
            Add(d, "hk_err_shiftonly", "Shift وحده لا يكفي لأنه يُستخدم في الكتابة العادية. أضف Ctrl أو Alt.", "Shift alone is not enough because it is used for normal typing. Add Ctrl or Alt.");
            Add(d, "hk_err_reserved", "هذه التركيبة محجوزة للنظام أو للنسخ واللصق ولا يمكن استخدامها. اختر تركيبة مختلفة.", "This combination is reserved by Windows or by copy and paste and cannot be used. Choose a different one.");
            Add(d, "hk_err_duplicate", "هذه التركيبة مستخدمة بالفعل في اختصار آخر داخل Nabra.", "This combination is already used by another Nabra shortcut.");
            Add(d, "hk_err_inuse", "هذا الاختصار مستخدم من تطبيق آخر. اختر تركيبة مختلفة.", "This shortcut is used by another app. Choose a different combination.");
            Add(d, "hk_err_register", "تعذّر تسجيل هذا الاختصار في Windows. اختر تركيبة مختلفة.", "Windows could not register this shortcut. Choose a different combination.");
            Add(d, "hk_warn_word", "تنبيه: Ctrl + K هو اختصار إدراج رابط في Word، وسيمنع Nabra Word من استخدامه.", "Heads-up: Ctrl + K inserts a link in Word, and Nabra will stop Word from receiving it.");
            Add(d, "hk_warn_common", "تنبيه: هذه التركيبة شائعة في التطبيقات وسيأخذها Nabra منها. يُفضَّل Ctrl + Alt.", "Heads-up: apps commonly use this combination and Nabra will take it from them. Ctrl + Alt is safer.");
            Add(d, "hk_warn_win", "تنبيه: يحجز Windows كثيرًا من تركيبات مفتاح Win، وقد لا تعمل بعضها.", "Heads-up: Windows reserves many Win-key combinations, so some may not work.");

            // ---- settings page
            Add(d, "set_subtitle", "كل التغييرات تُحفظ وتُطبَّق فورًا.", "Every change is saved and applied immediately.");
            Add(d, "set_general", "عام", "General");
            Add(d, "set_startup", "التشغيل مع Windows", "Start with Windows");
            Add(d, "set_startup_desc", "يبدأ Nabra في الخلفية عند تسجيل الدخول.", "Nabra starts in the background when you sign in.");
            Add(d, "set_tray", "الإغلاق إلى شريط المهام", "Close to the system tray");
            Add(d, "set_tray_desc", "عند إغلاق النافذة يستمر Nabra في العمل بجوار الساعة.", "Closing the window keeps Nabra running next to the clock.");
            Add(d, "set_notify", "إشعارات النجاح", "Success notifications");
            Add(d, "set_notify_desc", "إشعار صغير بعد كل تحويل أو عند تعذّره.", "A small notice after each conversion, or when it fails.");
            Add(d, "set_sound", "صوت عند النجاح", "Sound on success");
            Add(d, "set_sound_desc", "نغمة قصيرة بعد كل تحويل ناجح.", "A short sound after each successful conversion.");
            Add(d, "set_conversion", "التحويل", "Conversion");
            Add(d, "set_mode_desc", "اتجاه التحويل عند الضغط على الاختصار.", "Direction used when you press the shortcut.");
            Add(d, "set_restore", "استعادة الحافظة", "Restore the clipboard");
            Add(d, "set_restore_desc", "يعيد Nabra ما كان في الحافظة قبل التحويل.", "Nabra puts back whatever was on the clipboard before converting.");
            Add(d, "set_speed", "سرعة النسخ واللصق", "Copy and paste speed");
            Add(d, "set_speed_desc", "اختر «متأنٍّ» إذا كان التطبيق بطيئًا في الاستجابة.", "Choose Relaxed if an app is slow to respond.");
            Add(d, "speed_fast", "سريع", "Fast");
            Add(d, "speed_normal", "عادي", "Normal");
            Add(d, "speed_relaxed", "متأنٍّ", "Relaxed");
            Add(d, "set_appearance", "المظهر", "Appearance");
            Add(d, "set_theme", "السمة", "Theme");
            Add(d, "theme_system", "حسب النظام", "System");
            Add(d, "theme_light", "فاتح", "Light");
            Add(d, "theme_dark", "داكن", "Dark");
            Add(d, "set_language", "لغة الواجهة", "Interface language");
            Add(d, "set_size", "حجم الواجهة", "Interface size");
            Add(d, "size_small", "صغير", "Small");
            Add(d, "size_normal", "عادي", "Normal");
            Add(d, "size_large", "كبير", "Large");
            Add(d, "size_xlarge", "كبير جدًا", "Extra large");
            Add(d, "set_accent", "اللون الرئيسي", "Accent colour");
            Add(d, "set_accent_custom", "أو أدخل لونًا بصيغة HEX:", "Or enter a HEX colour:");
            Add(d, "set_data", "البيانات", "Data");
            Add(d, "set_data_desc", "تُحفظ الإعدادات في ملف JSON محلي فقط. لا قاعدة بيانات ولا سحابة.", "Settings are kept in a local JSON file only. No database, no cloud.");
            Add(d, "btn_open_folder", "فتح مجلد الإعدادات", "Open settings folder");
            Add(d, "btn_reset_all", "إعادة ضبط كل الإعدادات", "Reset all settings");
            Add(d, "reset_title", "إعادة الضبط", "Reset");
            Add(d, "reset_confirm", "سيتم إرجاع كل الإعدادات (ومنها الاختصار) إلى الوضع الافتراضي. هل تريد المتابعة؟", "All settings, including the shortcut, will return to their defaults. Continue?");

            // ---- real-time correction
            Add(d, "rt_section_title", "التصحيح اللحظي (ميزة اختيارية)", "Real-time correction (Optional)");
            Add(d, "rt_subtitle", "يصلح الكلمة تلقائيًا عند الضغط على Space أو Tab أو بعد التوقف عن الكتابة، دون الحاجة لتحديد النص أو ضغط اختصار.", "Auto-fixes words as you type upon Space, Tab or idle pause, without selecting text or pressing shortcuts.");
            Add(d, "rt_enable", "تفعيل التصحيح اللحظي", "Enable real-time correction");
            Add(d, "rt_enable_desc", "تصحيح الكلمة فور ضغط Space أو Tab (مثل: تحويل lvpfh إلى مرحبا، وlh hofhv; إلى ما اخبارك).", "Instantly fixes mistyped words upon pressing Space or Tab (e.g. lvpfh -> مرحبا).");
            Add(d, "rt_timeout_title", "مهلة التوقف قبل التصحيح", "Idle timeout before correction");
            Add(d, "rt_timeout_desc", "إذا انتهت الكتابة دون ضغط Space، ينتظر Nabra هذه المدة ثم يصحح الكلمة تلقائيًا (بالثواني).", "If typing pauses without pressing Space, Nabra waits this duration then auto-corrects the word.");
            Add(d, "rt_smart_title", "تجاوز الكلمات الإنجليزية الشائعة", "Ignore common English words");
            Add(d, "rt_smart_desc", "في الوضع التلقائي: يمنع تحويل الكلمات الإنجليزية الصحيحة (مثل hello وthis) لتجنب التحويل غير المقصود.", "In Auto mode: keeps valid English words (such as hello and this) from being converted.");
            Add(d, "rt_enter_title", "تصحيح عند ضغط Enter", "Correct on Enter");
            Add(d, "rt_enter_desc", "تصحيح الكلمة الأخيرة قبل إرسال الرسالة أو الانتقال لسطر جديد.", "Corrects the active word before sending a message or moving to a new line.");
            Add(d, "rt_tester_title", "اختبار التصحيح اللحظي والمهلة", "Test real-time correction & timeout");
            Add(d, "rt_tester_desc", "جرّب الكتابة هنا: اكتب lvpfh ثم اضغط Space، أو اكتب lh hofhv;، أو توقف عن الكتابة وانتظر المهلة لتشاهد التصحيح التلقائي مباشرة!", "Try typing here: type lvpfh then press Space, or type lh hofhv;, or stop typing and wait for the timeout!");
            Add(d, "rt_badge_active", "التصحيح اللحظي: مفعّل", "Real-time: On");
            Add(d, "rt_badge_inactive", "التصحيح اللحظي: متوقف", "Real-time: Off");

            // ---- elevation / admin
            Add(d, "elevate_card_title", "صلاحيات النظام والوصول الشامل", "System Privileges & Full Access");
            Add(d, "elevate_card_desc", "منح صلاحيات المسؤول يسمح لـ Nabra بالعمل بأعلى سرعة وتصحيح الكتابة في جميع التطبيقات والمستندات الإدارية دون قيود أمنية من ويندوز.", "Granting Administrator rights enables Nabra to run at top speed and correct text in all apps and elevated documents without Windows security blocks.");
            Add(d, "elevate_status_admin", "🛡️ يعمل بصلاحيات المسؤول (وصول كامل للنظام)", "🛡️ Running as Administrator (Full Access)");
            Add(d, "elevate_status_normal", "⚠️ يعمل كمستخدم عادي (قيود UIPI)", "⚠️ Running as standard user (UIPI limits)");
            Add(d, "elevate_btn", "⚡ تشغيل كمسؤول (وصول كامل)", "⚡ Run as Administrator (Full Access)");
            Add(d, "elevate_dialog_title", "طلب صلاحيات المسؤول (Administrator)", "Request Administrator Privileges");
            Add(d, "elevate_dialog_message", "هل توافق على منح Nabra صلاحيات المسؤول الكاملة والوصول إلى جذر النظام؟\n\nعند الموافقة، سيتم إعادة تشغيل التطبيق كمسؤول لتمكينه من تعقب لوحة المفاتيح والتصحيح اللحظي بسرعة قصوى في كافة نوافذ ومستندات النظام وتجاوز قيود واجهة المستخدم (UIPI) دون أي عوائق.\n\nملاحظة: تقع هذه الصلاحية على مسؤولية واختيار المستخدم بالكامل.", "Do you agree to grant Nabra full Administrator rights and system access?\n\nIf approved, the app will restart elevated to track keystrokes and perform real-time correction at maximum speed across all documents and apps without UIPI restrictions.\n\nNote: This permission is entirely your choice and responsibility.");

            // ---- about
            Add(d, "about_developer_title", "معلومات المطور", "Developer Information");
            Add(d, "about_developer_name", "Khalil Mohammad Khalil", "Khalil Mohammad Khalil");
            Add(d, "about_developer_email", "E-mail : khalilmkhalil0937@gmail.com", "E-mail : khalilmkhalil0937@gmail.com");
            Add(d, "about_how_title", "كيف يعمل؟", "How does it work?");
            Add(d, "about_how_body", "1) اكتب نصًا بلوحة مفاتيح بلغة خاطئة، مثل lvpfh بدل مرحبا.\n2) حدّد النص واضغط اختصار Nabra (أو فعّل التصحيح اللحظي ليصحح تلقائيًا أثناء الكتابة).\n3) يستبدله Nabra بالنص الصحيح ويعيد الحافظة كما كانت.\nلا ترجمة ولا ذكاء اصطناعي: يُحوَّل كل حرف حسب موضعه على لوحة المفاتيح.", "1) Type text with the wrong keyboard language, for example lvpfh instead of مرحبا.\n2) Select it and press the Nabra shortcut (or enable Real-Time Correction to fix as you type).\n3) Nabra replaces it with the right text and puts the clipboard back.\nNo translation and no AI: each letter is converted by its key position.");
            Add(d, "about_privacy_title", "الخصوصية", "Privacy");
            Add(d, "about_privacy_body", "• لا اتصال بالإنترنت ولا حساب ولا خادم.\n• لا تُحفظ النصوص ولا تُسجَّل في أي ملف.\n• لا تُسجَّل ضغطات المفاتيح العادية في سجل دائم.\n• يعمل دون صلاحيات المسؤول.\n• النص المحوَّل لا يُحفظ في سجل الحافظة ولا يُزامَن مع السحابة.", "• No internet connection, account or server.\n• Text is never stored or written to any log.\n• Ordinary keystrokes are never saved to disk.\n• Runs without administrator rights.\n• Converted text is kept out of Windows Clipboard History and cloud sync.");
            Add(d, "about_layout_title", "ملاحظة عن التخطيط", "Layout note");
            Add(d, "about_layout_body", "يعتمد التحويل على تخطيط Windows «Arabic (101)». الأرقام والعلامات المشتركة (! @ # $ % ( ) - =) لا تتغير. بعض الأحرف العربية موجودة على مفاتيح الترقيم (ك على ; وط على ' وو على , وز على . وظ على /) لذلك تتحول هذه العلامات أيضًا.", "Conversion follows the Windows Arabic (101) layout. Digits and shared symbols (! @ # $ % ( ) - =) never change. Some Arabic letters sit on punctuation keys (ك on ;  ط on '  و on ,  ز on .  ظ on /), so those marks convert too.");

            // ---- tray
            Add(d, "tray_enable", "تفعيل Nabra", "Turn Nabra on");
            Add(d, "tray_disable", "إيقاف Nabra", "Turn Nabra off");
            Add(d, "tray_realtime", "التصحيح اللحظي", "Real-time correction");
            Add(d, "tray_open", "فتح الواجهة", "Open window");
            Add(d, "tray_change_hotkey", "تغيير الاختصار", "Change shortcut");
            Add(d, "tray_mode", "وضع التحويل", "Conversion mode");
            Add(d, "tray_settings", "الإعدادات", "Settings");
            Add(d, "tray_exit", "الخروج", "Exit");

            // ---- toasts
            Add(d, "toast_enabled", "تم تفعيل Nabra.", "Nabra is on.");
            Add(d, "toast_disabled", "تم إيقاف Nabra.", "Nabra is off.");
            Add(d, "toast_ok_ar", "تم التحويل إلى العربية.", "Converted to Arabic.");
            Add(d, "toast_ok_en", "تم التحويل إلى الإنجليزية.", "Converted to English.");
            Add(d, "toast_no_selection", "لم يتم العثور على نص محدد. حدّد نصًا ثم أعد المحاولة.", "No selected text was found. Select some text and try again.");
            Add(d, "toast_nothing", "لا يوجد ما يحتاج إلى تحويل في النص المحدد.", "Nothing to convert in the selected text.");
            Add(d, "toast_error", "تعذّر إتمام التحويل. حاول مرة أخرى.", "The conversion could not be completed. Please try again.");
            Add(d, "toast_in_tray", "ما زال Nabra يعمل في الخلفية. للخروج استخدم أيقونة شريط المهام.", "Nabra is still running in the background. Use the tray icon to exit.");

            return d;
        }
    }
}
