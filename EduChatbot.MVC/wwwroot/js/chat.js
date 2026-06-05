// EduChatbot chat composer: AJAX send, markdown rendering, loading state.
(function () {
    'use strict';

    initChatShell();

    const messagesContainer = document.getElementById('chat-messages');
    const chatForm = document.getElementById('chat-form');
    const chatInput = document.getElementById('chat-input');
    const sendBtn = document.getElementById('chat-send-btn');
    const conversationId = document.getElementById('conversation-id')?.value;
    const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    if (!messagesContainer || !chatForm || !chatInput || !sendBtn) return;

    let busy = false;

    chatInput.addEventListener('input', function () {
        this.style.height = 'auto';
        this.style.height = Math.min(this.scrollHeight, 160) + 'px';
        updateSendBtn();
    });

    chatInput.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            submitMessage();
        }
    });

    chatForm.addEventListener('submit', function (e) {
        e.preventDefault();
        submitMessage();
    });

    document.querySelectorAll('.suggestion-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            chatInput.value = this.textContent.trim();
            chatInput.dispatchEvent(new Event('input'));
            submitMessage();
        });
    });

    function updateSendBtn() {
        sendBtn.disabled = busy || !chatInput.value.trim();
    }

    function submitMessage() {
        const text = chatInput.value.trim();
        if (!text || busy) return;

        busy = true;
        updateSendBtn();

        const welcome = document.getElementById('chat-welcome');
        if (welcome) welcome.remove();

        ensureMessageStack();
        appendMessage('user', text);

        const loadingId = 'loading-' + Date.now();
        appendLoading(loadingId);

        chatInput.value = '';
        chatInput.style.height = 'auto';
        scrollToBottom();

        fetch('/Chat/SendMessage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': antiForgeryToken
            },
            body: 'conversationId=' + encodeURIComponent(conversationId) +
                '&message=' + encodeURIComponent(text) +
                '&__RequestVerificationToken=' + encodeURIComponent(antiForgeryToken)
        })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                removeLoading(loadingId);
                appendMessage('ai', data.content, data.sources);
                scrollToBottom();
            })
            .catch(function () {
                removeLoading(loadingId);
                appendMessage('ai', window.EduI18n ? EduI18n.t('chat.errorSending') : 'Sorry, an error occurred while sending the message. Please try again.');
                scrollToBottom();
            })
            .finally(function () {
                busy = false;
                updateSendBtn();
            });
    }

    function ensureMessageStack() {
        let stack = messagesContainer.querySelector('.chat-message-stack');
        if (!stack) {
            stack = document.createElement('div');
            stack.className = 'chat-message-stack';
            messagesContainer.appendChild(stack);
        }
        return stack;
    }

    function appendMessage(role, content, sources) {
        const row = document.createElement('div');
        row.className = 'msg-row ' + role;

        if (role === 'ai') {
            const avatar = document.createElement('div');
            avatar.className = 'msg-avatar ai';
            avatar.setAttribute('aria-hidden', 'true');
            avatar.textContent = 'AI';
            row.appendChild(avatar);
        }

        const contentDiv = document.createElement('div');
        contentDiv.className = 'msg-content';

        const bubble = document.createElement('div');
        bubble.className = 'msg-bubble ' + role;
        if (role === 'ai') {
            bubble.innerHTML = renderMarkdown(content);
        } else {
            bubble.textContent = content;
        }
        contentDiv.appendChild(bubble);

        if (role === 'ai' && sources && sources.length > 0) {
            const sourcesDiv = document.createElement('div');
            sourcesDiv.className = 'msg-sources';
            sourcesDiv.innerHTML = '<div class="sources-label">Sources</div>';

            const tagsDiv = document.createElement('div');
            tagsDiv.className = 'source-list';
            sources.forEach(function (s) {
                const tag = document.createElement('span');
                tag.className = 'source-tag';
                tag.innerHTML = '<svg viewBox="0 0 24 24" focusable="false" aria-hidden="true">' +
                    '<path d="M7 3.5h6.5L18 8v12.5H7V3.5Z" />' +
                    '<path d="M13.5 3.5V8H18" />' +
                    '</svg>' +
                    '<span>' + escapeHtml(s.doc) + '</span><i aria-hidden="true">&middot;</i><span>Chunk ' + s.chunk + '</span>';
                tagsDiv.appendChild(tag);
            });
            sourcesDiv.appendChild(tagsDiv);
            contentDiv.appendChild(sourcesDiv);
        }

        row.appendChild(contentDiv);
        ensureMessageStack().appendChild(row);
    }

    function appendLoading(id) {
        const row = document.createElement('div');
        row.className = 'msg-row ai';
        row.id = id;

        const avatar = document.createElement('div');
        avatar.className = 'msg-avatar ai';
        avatar.setAttribute('aria-hidden', 'true');
        avatar.textContent = 'AI';

        const contentDiv = document.createElement('div');
        contentDiv.className = 'msg-content';

        const bubble = document.createElement('div');
        bubble.className = 'msg-bubble ai';
        bubble.innerHTML = '<div class="loading-dots">' +
            '<div class="dots"><div class="dot"></div><div class="dot"></div><div class="dot"></div></div>' +
            '<span class="loading-text">' + (window.EduI18n ? EduI18n.t('chat.searchingDocs') : 'Searching documents...') + '</span>' +
            '</div>';

        contentDiv.appendChild(bubble);
        row.appendChild(avatar);
        row.appendChild(contentDiv);
        ensureMessageStack().appendChild(row);
    }

    function removeLoading(id) {
        const el = document.getElementById(id);
        if (el) el.remove();
    }

    function scrollToBottom() {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text || '';
        return div.innerHTML;
    }

    function renderMarkdown(text) {
        if (!text) return '';
        let html = escapeHtml(text);

        html = html.replace(/```([\s\S]*?)```/g, function (_, code) {
            return '<pre class="md-code-block"><code>' + code.trim() + '</code></pre>';
        });

        html = html.replace(/`([^`]+)`/g, '<code class="md-inline-code">$1</code>');
        html = html.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
        html = html.replace(/(?<!\*)\*([^*]+)\*(?!\*)/g, '<em>$1</em>');

        const lines = html.split('\n');
        const result = [];
        let inList = false;
        let listType = '';

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const trimmed = line.trim();

            if (/^[-*]\s+/.test(trimmed)) {
                if (!inList || listType !== 'ul') {
                    if (inList) result.push('</' + listType + '>');
                    result.push('<ul class="md-list">');
                    inList = true;
                    listType = 'ul';
                }
                result.push('<li>' + trimmed.replace(/^[-*]\s+/, '') + '</li>');
            } else if (/^\d+\.\s+/.test(trimmed)) {
                if (!inList || listType !== 'ol') {
                    if (inList) result.push('</' + listType + '>');
                    result.push('<ol class="md-list">');
                    inList = true;
                    listType = 'ol';
                }
                result.push('<li>' + trimmed.replace(/^\d+\.\s+/, '') + '</li>');
            } else {
                if (inList) {
                    result.push('</' + listType + '>');
                    inList = false;
                    listType = '';
                }

                if (trimmed === '') {
                    result.push('<br />');
                } else {
                    result.push('<p class="md-paragraph">' + line + '</p>');
                }
            }
        }

        if (inList) result.push('</' + listType + '>');
        return result.join('');
    }

    scrollToBottom();
    updateSendBtn();

    function initChatShell() {
        if (!document.body.classList.contains('chat-layout')) return;

        const storageKey = 'eduChatbot.sidebarCollapsed';
        const toggle = document.querySelector('.chat-sidebar-toggle');
        const shell = document.querySelector('.chat-shell');

        try {
            if (localStorage.getItem(storageKey) === 'true') {
                document.body.classList.add('chat-sidebar-collapsed');
                document.documentElement.classList.add('chat-sidebar-collapsed-persisted');
            } else {
                document.documentElement.classList.remove('chat-sidebar-collapsed-persisted');
            }
        } catch {
            // localStorage can be unavailable in private or locked-down contexts.
            document.documentElement.classList.remove('chat-sidebar-collapsed-persisted');
        }

        window.requestAnimationFrame(function () {
            document.body.classList.add('chat-sidebar-ready');
        });

        if (toggle) {
            updateToggleLabel();
            toggle.addEventListener('click', function () {
                const isCollapsed = document.body.classList.toggle('chat-sidebar-collapsed');
                document.documentElement.classList.toggle('chat-sidebar-collapsed-persisted', isCollapsed);
                try {
                    localStorage.setItem(storageKey, isCollapsed ? 'true' : 'false');
                } catch {
                    // Ignore persistence failures; the visible toggle still works.
                }
                updateToggleLabel();
            });
        }

        document.querySelectorAll('.chat-nav-action, .chat-sidebar-item, .chat-brand-link').forEach(function (link) {
            link.addEventListener('click', function (event) {
                const href = this.getAttribute('href');
                if (!href || href === '#') {
                    event.preventDefault();
                    return;
                }

                const target = new URL(href, window.location.origin);
                if (target.pathname === window.location.pathname && target.search === window.location.search) {
                    event.preventDefault();
                    return;
                }

                shell?.classList.add('chat-shell-leaving');
            });
        });

        function updateToggleLabel() {
            const isCollapsed = document.body.classList.contains('chat-sidebar-collapsed');
            toggle?.setAttribute('aria-label', isCollapsed ? 'Expand sidebar' : 'Collapse sidebar');
            toggle?.setAttribute('aria-expanded', isCollapsed ? 'false' : 'true');
        }
    }
})();
