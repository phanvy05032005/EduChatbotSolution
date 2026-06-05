/* ========================================================
   EduChatbot — i18n (English ↔ Vietnamese)
   Default language: English (all HTML text is in English).
   Vietnamese translations are applied via JS when user toggles.
   ======================================================== */

(function () {
    'use strict';

    /* ---------- Vietnamese translations ---------- */
    const VI = {
        /* ---- Shared / Layout ---- */
        'nav.chat': 'Chat',
        'nav.profile': 'Hồ sơ',
        'nav.logout': 'Đăng xuất',
        'nav.login': 'Đăng nhập',
        'nav.register': 'Đăng ký',
        'nav.dashboard': 'Bảng điều khiển',
        'nav.documents': 'Tài liệu',
        'nav.upload': 'Tải lên',
        'nav.learningMaterials': 'Tài liệu học tập',
        'nav.uploadDocument': 'Tải lên tài liệu',

        /* ---- Admin Sidebar ---- */
        'admin.sidebar.dashboard': 'Bảng điều khiển',
        'admin.sidebar.studentAccounts': 'Tài khoản sinh viên',
        'admin.sidebar.lecturerAccounts': 'Tài khoản giảng viên',
        'admin.sidebar.coursesAssignments': 'Môn học & Phân công',
        'admin.sidebar.pendingReview': 'Chờ duyệt',
        'admin.sidebar.roleManagement': 'Quản lý vai trò',
        'admin.sidebar.systemManagement': 'Quản lý hệ thống',
        'admin.sidebar.statistics': 'Thống kê',
        'admin.sidebar.logout': 'Đăng xuất',

        /* ---- Admin Dashboard ---- */
        'admin.dashboard.kicker': 'Tổng quan hệ thống',
        'admin.dashboard.title': 'Tổng quan',
        'admin.dashboard.titleSerif': 'hệ thống',
        'admin.dashboard.subtitle': 'Theo dõi số liệu và hoạt động trên toàn hệ thống EduChatbot.',
        'admin.dashboard.refresh': 'Làm mới',
        'admin.dashboard.recentActivities': 'Hoạt động gần đây',
        'admin.dashboard.live': 'Trực tiếp',
        'admin.dashboard.updatedJustNow': 'Vừa cập nhật · Quản trị',
        'admin.dashboard.questionsLast7': 'Câu hỏi · 7 ngày qua',
        'admin.dashboard.totalQuestions': 'Tổng câu hỏi',
        'admin.dashboard.totalStudents': 'Tổng sinh viên',
        'admin.dashboard.totalLecturers': 'Tổng giảng viên',
        'admin.dashboard.documentsInSystem': 'Tài liệu trong hệ thống',
        'admin.dashboard.totalChatQuestions': 'Tổng câu hỏi Chat',

        /* ---- Admin Accounts ---- */
        'admin.accounts.manage': 'Quản lý',
        'admin.accounts.createAccount': 'Tạo tài khoản',
        'admin.accounts.viewSearchDesc': 'Xem, tìm kiếm, khóa, mở khóa và quản lý tài khoản',
        'admin.accounts.importStudentExcel': 'Nhập tài khoản sinh viên bằng Excel',
        'admin.accounts.importLecturerExcel': 'Nhập tài khoản giảng viên bằng Excel',
        'admin.accounts.uploadImport': 'Tải lên & Nhập',
        'admin.accounts.requireStudentExcel': 'Yêu cầu file Excel (.xlsx) có chứa các cột: Email và FullName. Hệ thống sẽ tự động gửi thông tin tài khoản đến email sinh viên được import.',
        'admin.accounts.requireLecturerExcel': 'Yêu cầu file Excel (.xlsx) có chứa các cột: FullName, Email, CourseCodes.',
        'admin.accounts.exampleCodes': 'Ví dụ CourseCodes: PRN222 hoặc PRN222,PRM392. Email sẽ được đưa vào hàng đợi gửi nền.',
        'admin.accounts.searchPlaceholder': 'Tìm theo ID, họ tên hoặc email',
        'admin.accounts.list': 'Danh sách',
        'admin.accounts.accounts': 'tài khoản',
        'admin.accounts.lecturerId': 'ID Giảng viên',
        'admin.accounts.studentId': 'ID Sinh viên',
        'admin.accounts.fullName': 'Họ tên',
        'admin.accounts.email': 'Email',
        'admin.accounts.department': 'Khoa',
        'admin.accounts.status': 'Trạng thái',
        'admin.accounts.actions': 'Thao tác',
        'admin.accounts.edit': 'Sửa',
        'admin.accounts.lock': 'Khóa',
        'admin.accounts.unlock': 'Mở khóa',
        'admin.accounts.delete': 'Xóa',
        'admin.accounts.noMatching': 'Không tìm thấy tài khoản phù hợp.',
        'admin.accounts.search': 'Tìm kiếm',
        'admin.accounts.confirmDelete': 'Xóa tài khoản này?',

        /* ---- Admin Account Form ---- */
        'admin.form.edit': 'Chỉnh sửa',
        'admin.form.create': 'Tạo mới',
        'admin.form.account': 'tài khoản',
        'admin.form.maintainDesc': 'Quản lý thông tin danh tính',
        'admin.form.fullName': 'Họ tên',
        'admin.form.password': 'Mật khẩu',
        'admin.form.assignedCourses': 'Môn học phụ trách',
        'admin.form.searchCourses': 'Tìm theo mã hoặc tên môn (ví dụ: PRN222, .NET...)',
        'admin.form.noCourses': 'Chưa có môn học nào được tạo. Hãy tạo môn học trước.',
        'admin.form.holdToSelect': 'Giữ phím Cmd (macOS) / Ctrl (Windows) để chọn nhiều môn.',
        'admin.form.autoSendEmail': 'Hệ thống sẽ tự động gửi thông tin tài khoản đến email người dùng sau khi tạo thành công.',
        'admin.form.save': 'Lưu tài khoản',
        'admin.form.cancel': 'Hủy',

        /* ---- Admin Courses ---- */
        'admin.courses.kicker': 'Môn học',
        'admin.courses.title': 'Quản lý',
        'admin.courses.titleSerif': 'Môn học & Phân công',
        'admin.courses.subtitle': 'Tạo môn học mới và phân công quyền đăng tải tài liệu cho từng giảng viên.',
        'admin.courses.createNew': 'Tạo Môn học mới',
        'admin.courses.courseCode': 'Mã môn học',
        'admin.courses.courseCodePlaceholder': 'Ví dụ: PRN222',
        'admin.courses.courseName': 'Tên môn học',
        'admin.courses.courseNamePlaceholder': 'Ví dụ: C# & .NET Cloud',
        'admin.courses.courseDesc': 'Mô tả môn học',
        'admin.courses.courseDescPlaceholder': 'Ví dụ: ASP.NET Core, MVC, Entity Framework Core, Repository Pattern, Dependency Injection, Authentication, Authorization',
        'admin.courses.createCourse': 'Tạo môn học',
        'admin.courses.importExcel': 'Nhập Môn học bằng Excel',
        'admin.courses.importCourses': 'Nhập môn học',
        'admin.courses.downloadTemplate': 'Tải mẫu Excel',
        'admin.courses.requireExcel': 'Yêu cầu file Excel (.xlsx) chứa các cột: Code (Mã môn học), Name (Tên môn học), và Description (Mô tả).',
        'admin.courses.courseList': 'Danh sách môn học',
        'admin.courses.courses': 'môn học',
        'admin.courses.code': 'Mã Môn',
        'admin.courses.name': 'Tên Môn',
        'admin.courses.assignedLecturers': 'Giảng viên phụ trách',
        'admin.courses.noDesc': 'Chưa có mô tả môn học.',
        'admin.courses.noLecturer': 'Chưa phân công giảng viên',
        'admin.courses.selectLecturer': '-- Chọn giảng viên --',
        'admin.courses.assign': 'Phân công',
        'admin.courses.confirmDelete': 'Xóa môn học này sẽ làm mất liên kết với các tài liệu đã tải lên?',

        /* ---- Admin Roles ---- */
        'admin.roles.title': 'Phân',
        'admin.roles.titleSerif': 'quyền',
        'admin.roles.subtitle': 'Các vai trò trong hệ thống và phạm vi quyền hạn tương ứng.',
        'admin.roles.student': 'Sinh viên',
        'admin.roles.lecturer': 'Giảng viên',
        'admin.roles.admin': 'Quản trị viên',
        'admin.roles.studentDesc': 'Hỏi đáp về tài liệu đã upload, xem lịch sử trò chuyện cá nhân.',
        'admin.roles.lecturerDesc': 'Upload, quản lý và chỉnh sửa tài liệu giảng dạy.',
        'admin.roles.adminDesc': 'Toàn quyền quản trị hệ thống và tài khoản.',
        'admin.roles.permissions': 'Quyền hạn',

        /* ---- Admin System ---- */
        'admin.system.title': 'Quản lý',
        'admin.system.titleSerif': 'hệ thống',
        'admin.system.subtitle': 'Trạng thái các dịch vụ và cấu hình hệ thống.',
        'admin.system.refreshStatus': 'Làm mới trạng thái',
        'admin.system.backupSystem': 'Sao lưu hệ thống',
        'admin.system.dbStatus': 'Trạng thái Database',
        'admin.system.appStatus': 'Trạng thái ứng dụng',
        'admin.system.storageUsage': 'Dung lượng lưu trữ',
        'admin.system.sysVersion': 'Phiên bản hệ thống',
        'admin.system.dbDetail': 'Kết nối PostgreSQL',
        'admin.system.appDetail': 'ASP.NET Core MVC',
        'admin.system.storageDetail': 'Tài liệu đã upload',
        'admin.system.versionDetail': 'Assembly hiện tại',
        'admin.system.aiConfig': 'Cấu hình AI',
        'admin.system.runtime': 'Chạy thực tế',
        'admin.system.storage': 'Bộ nhớ lưu trữ',
        'admin.system.model': 'Mô hình AI',
        'admin.system.temp': 'Độ sáng tạo (Temp)',
        'admin.system.maxTokens': 'Token tối đa',
        'admin.system.embedding': 'Embedding / Vector',
        'admin.system.docPipeline': 'Xử lý tài liệu',
        'admin.system.chunkSize': 'Kích thước đoạn (Chunk)',
        'admin.system.overlap': 'Độ trùng lặp (Overlap)',
        'admin.system.formats': 'Định dạng hỗ trợ',
        'admin.system.database': 'Cơ sở dữ liệu',
        'admin.system.connected': 'Đã kết nối',
        'admin.system.disconnected': 'Mất kết nối',
        'admin.system.openrouterConfig': 'Đã cấu hình OpenRouter',
        'admin.system.vectorEnabled': 'Đã bật tìm kiếm Vector',
        'admin.system.chunkSizeDetail': '512 token',
        'admin.system.overlapDetail': '64 token',
        'admin.system.formatsDetail': 'PDF, DOCX',

        /* ---- Admin Statistics ---- */
        'admin.stats.title': 'Thống kê',
        'admin.stats.titleSerif': 'hệ thống',
        'admin.stats.subtitle': 'Hoạt động sử dụng và xu hướng dữ liệu tổng quan.',
        'admin.stats.totals': 'Số liệu tổng quan',
        'admin.stats.snapshot': 'Xem nhanh hiện tại',
        'admin.stats.topTopics': 'Chủ đề hàng đầu',
        'admin.stats.relativeActivity': 'Tỷ lệ hoạt động',
        'admin.stats.totalQuestionsAsked': 'Tổng số câu hỏi đã hỏi',
        'admin.stats.totalDocuments': 'Tổng số tài liệu',

        /* ---- Login ---- */
        'login.hero.line1': 'Học tập thông minh',
        'login.hero.line2': 'cùng tài liệu của bạn.',
        'login.hero.desc': 'Hỏi đáp tức thì về tài liệu giảng viên đã upload, kèm trích dẫn nguồn rõ ràng theo từng đoạn.',
        'login.stat.documents': 'Tài liệu',
        'login.stat.qa': 'Hỏi đáp',
        'login.stat.citations': 'Trích nguồn',
        'login.welcome': 'Chào mừng quay lại',

        /* ---- Chat ---- */
        'chat.welcome': 'Hôm nay bạn muốn học gì?',
        'chat.suggestion1': 'ASP.NET Core MVC là gì?',
        'chat.suggestion2': 'Giải thích Entity Framework Core',
        'chat.suggestion3': 'Repository Pattern hoạt động ra sao?',
        'chat.suggestion4': 'So sánh SQL Server và PostgreSQL',
        'chat.inputPlaceholder': 'Hỏi về tài liệu học tập...',
        'chat.aiDisclaimer': 'AI có thể mắc lỗi. Hãy đối chiếu với tài liệu gốc.',
        'chat.conversations': 'Cuộc trò chuyện',
        'chat.heroSubtitle': 'Học tập, khám phá, với tài liệu của bạn.',
        'chat.startNew': 'Bắt đầu Chat mới',
        'chat.selectCourseDesc': 'Chọn môn học để giới hạn phạm vi thảo luận của AI đúng tài liệu học tập bên trong môn đó.',
        'chat.selectCourse': '-- Chọn Môn học --',
        'chat.createChat': 'Tạo Chat',
        'chat.noConversations': 'Bạn chưa có cuộc trò chuyện nào. Hãy tạo cuộc trò chuyện mới để bắt đầu khám phá tài liệu.',
        'chat.noConversationsShort': 'Chưa có cuộc trò chuyện nào.',
        'chat.errorSending': 'Xin lỗi, đã xảy ra lỗi khi gửi tin nhắn. Vui lòng thử lại.',
        'chat.searchingDocs': 'Đang tìm kiếm tài liệu...',

        /* ---- Documents (Lecturer) ---- */
        'docs.dashboard.viewFull': 'Xem danh sách đầy đủ, kiểm tra chunk data và quản lý học liệu đã upload.',
        'docs.dashboard.addNew': 'Thêm tài liệu mới',
        'docs.dashboard.fileFormats': 'PDF, DOCX, tối đa 10 MB',
        'docs.details.course': 'Môn học',
        'docs.details.matchScore': 'Kết quả Match Score:',
        'docs.details.noChunks': 'Document này chưa có chunk data.',
        'docs.index.noDocuments': 'Chưa có tài liệu nào được upload.',
        'docs.index.uploadHint': 'Upload file PDF hoặc DOCX để hệ thống extract, chunk và tạo embedding.',
        'docs.index.course': 'Môn học',
        'docs.upload.course': 'Môn học',
        'docs.upload.selectCourse': '-- Chọn môn học --',
        'docs.upload.fileTooLarge': 'File không được vượt quá 10 MB.',

        /* ---- Profile ---- */
        'profile.kicker': 'HỒ SƠ CÁ NHÂN',
        'profile.title': 'Hồ sơ của bạn',
        'profile.back': '← Quay lại Chat',
        'profile.updateSuccess': 'Cập nhật hồ sơ thành công.',
        'profile.personalInfo': 'Thông tin cá nhân',
        'profile.changePassword': 'Đổi mật khẩu',
        'profile.lecturerAccount': 'Tài khoản giảng viên',
        'profile.studentAccount': 'Tài khoản sinh viên',
        'profile.backDashboard': 'Quay lại bảng điều khiển',
        'profile.backChat': 'Quay lại Chat',
        'profile.fullName': 'Họ tên',
        'profile.fullNamePlaceholder': 'Nhập họ tên của bạn',
        'profile.email': 'Email',
        'profile.currentPassword': 'Mật khẩu hiện tại',
        'profile.newPassword': 'Mật khẩu mới',
        'profile.updateProfile': 'Cập nhật hồ sơ',
        'profile.changePasswordBtn': 'Đổi mật khẩu',
        'profile.confirmPassword': 'Xác nhận mật khẩu mới',

        /* ---- Pending Review ---- */
        'pending.title': 'Chờ duyệt',
        'admin.pending.title': 'Duyệt tài liệu',
        'admin.pending.titleSerif': 'Chờ duyệt',
        'admin.pending.subtitle': 'Duyệt các tài liệu có điểm khớp môn học dưới ngưỡng tự động duyệt.',
        'admin.pending.empty': 'Không có tài liệu nào đang chờ duyệt.',
        'admin.pending.heading': 'Tài liệu đang chờ quyết định',
        'admin.pending.pendingCount': 'đang chờ',
        'admin.pending.fileName': 'Tên file',
        'admin.pending.lecturer': 'Giảng viên',
        'admin.pending.subject': 'Môn học',
        'admin.pending.matchScore': 'Match Score',
        'admin.pending.uploadDate': 'Ngày tải lên',
        'admin.pending.reason': 'Lý do',
        'admin.pending.approve': 'Duyệt',
        'admin.pending.reject': 'Từ chối',
    };

    /* ---------- Core logic ---------- */
    const STORAGE_KEY = 'eduChatbot.lang';
    const COOKIE_NAME = 'edu_lang';

    function getLang() {
        return localStorage.getItem(STORAGE_KEY) || 'en';
    }

    function setLang(lang) {
        localStorage.setItem(STORAGE_KEY, lang);
        document.cookie = COOKIE_NAME + '=' + lang + ';path=/;max-age=31536000';
        applyLang(lang);
    }

    function applyLang(lang) {
        document.documentElement.lang = lang;

        // Translate data-i18n elements
        document.querySelectorAll('[data-i18n]').forEach(function (el) {
            var key = el.getAttribute('data-i18n');
            if (lang === 'vi' && VI[key]) {
                // Store original English text
                if (!el.hasAttribute('data-i18n-en')) {
                    el.setAttribute('data-i18n-en', el.textContent);
                }
                el.textContent = VI[key];
            } else {
                // Restore English
                var en = el.getAttribute('data-i18n-en');
                if (en !== null) {
                    el.textContent = en;
                }
            }
        });

        // Translate data-i18n-placeholder
        document.querySelectorAll('[data-i18n-placeholder]').forEach(function (el) {
            var key = el.getAttribute('data-i18n-placeholder');
            if (lang === 'vi' && VI[key]) {
                if (!el.hasAttribute('data-i18n-placeholder-en')) {
                    el.setAttribute('data-i18n-placeholder-en', el.placeholder);
                }
                el.placeholder = VI[key];
            } else {
                var en = el.getAttribute('data-i18n-placeholder-en');
                if (en !== null) {
                    el.placeholder = en;
                }
            }
        });

        // Translate data-i18n-confirm (onclick confirm dialogs)
        document.querySelectorAll('[data-i18n-confirm]').forEach(function (el) {
            var key = el.getAttribute('data-i18n-confirm');
            if (lang === 'vi' && VI[key]) {
                el.setAttribute('onclick', "return confirm('" + VI[key].replace(/'/g, "\\'") + "');");
            } else {
                var en = el.getAttribute('data-i18n-confirm-en');
                if (en) {
                    el.setAttribute('onclick', "return confirm('" + en.replace(/'/g, "\\'") + "');");
                }
            }
        });

        // Update toggle button text
        var toggleBtns = document.querySelectorAll('.lang-toggle-btn');
        toggleBtns.forEach(function (btn) {
            btn.textContent = lang === 'vi' ? 'EN' : 'VI';
            btn.title = lang === 'vi' ? 'Switch to English' : 'Chuyển sang Tiếng Việt';
        });
    }

    /* ---------- Public API ---------- */
    window.EduI18n = {
        getLang: getLang,
        setLang: setLang,
        toggle: function () {
            setLang(getLang() === 'en' ? 'vi' : 'en');
        },
        t: function (key) {
            var lang = getLang();
            if (lang === 'vi' && VI[key]) return VI[key];
            return key; // fallback
        }
    };

    /* ---------- Auto-apply on load ---------- */
    document.addEventListener('DOMContentLoaded', function () {
        applyLang(getLang());
    });
})();
