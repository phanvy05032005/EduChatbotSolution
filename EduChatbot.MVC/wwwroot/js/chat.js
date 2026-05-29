// EduChatbot — Chat JS (AJAX send message, render bubbles, auto-scroll)
(function () {
    'use strict';

    const messagesContainer = document.getElementById('chat-messages');
    const chatForm = document.getElementById('chat-form');
    const chatInput = document.getElementById('chat-input');
    const sendBtn = document.getElementById('chat-send-btn');
    const conversationId = document.getElementById('conversation-id')?.value;
    const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    if (!chatForm || !chatInput) return;

    let busy = false;

    // Auto-resize textarea
    chatInput.addEventListener('input', function () {
        this.style.height = 'auto';
        this.style.height = Math.min(this.scrollHeight, 160) + 'px';
        updateSendBtn();
    });

    // Enter to send (Shift+Enter for new line)
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

    // Suggestion buttons
    document.querySelectorAll('.suggestion-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            chatInput.value = this.dataset.question;
            chatInput.dispatchEvent(new Event('input'));
            submitMessage();
        });
    });

    function updateSendBtn() {
        sendBtn.disabled = busy || !chatInput.value.trim();
    }

    function submitMessage() {
        var text = chatInput.value.trim();
        if (!text || busy) return;

        busy = true;
        updateSendBtn();

        // Hide welcome screen if visible
        var welcome = document.getElementById('chat-welcome');
        if (welcome) welcome.style.display = 'none';

        // Add user message bubble
        appendMessage('user', text);

        // Add loading indicator
        var loadingId = 'loading-' + Date.now();
        appendLoading(loadingId);

        // Clear input
        chatInput.value = '';
        chatInput.style.height = 'auto';
        scrollToBottom();

        // AJAX call
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
            appendMessage('ai', 'Xin lỗi, đã xảy ra lỗi khi gửi tin nhắn. Vui lòng thử lại.');
            scrollToBottom();
        })
        .finally(function () {
            busy = false;
            updateSendBtn();
        });
    }

    function appendMessage(role, content, sources) {
        var row = document.createElement('div');
        row.className = 'msg-row ' + role;

        var avatar = document.createElement('div');
        avatar.className = 'msg-avatar ' + role;
        avatar.textContent = role === 'ai' ? '✦' : '👤';

        var contentDiv = document.createElement('div');
        contentDiv.className = 'msg-content';

        var bubble = document.createElement('div');
        bubble.className = 'msg-bubble ' + role;
        if (role === 'ai') {
            bubble.innerHTML = renderMarkdown(content);
        } else {
            bubble.textContent = content;
        }
        contentDiv.appendChild(bubble);

        // Sources
        if (role === 'ai' && sources && sources.length > 0) {
            var sourcesDiv = document.createElement('div');
            sourcesDiv.className = 'msg-sources';
            sourcesDiv.innerHTML = '<div class="sources-label">Sources</div>';

            var tagsDiv = document.createElement('div');
            sources.forEach(function (s) {
                var tag = document.createElement('span');
                tag.className = 'source-tag';
                tag.innerHTML = '📄 ' + escapeHtml(s.doc) +
                    ' <span class="sep">·</span> <span class="chunk-label">Chunk ' + s.chunk + '</span>';
                tagsDiv.appendChild(tag);
            });
            sourcesDiv.appendChild(tagsDiv);
            contentDiv.appendChild(sourcesDiv);
        }

        if (role === 'user') {
            row.appendChild(contentDiv);
            row.appendChild(avatar);
        } else {
            row.appendChild(avatar);
            row.appendChild(contentDiv);
        }

        messagesContainer.appendChild(row);
    }

    function appendLoading(id) {
        var row = document.createElement('div');
        row.className = 'msg-row ai';
        row.id = id;

        var avatar = document.createElement('div');
        avatar.className = 'msg-avatar ai';
        avatar.textContent = '✦';

        var contentDiv = document.createElement('div');
        contentDiv.className = 'msg-content';

        var bubble = document.createElement('div');
        bubble.className = 'msg-bubble ai';
        bubble.innerHTML = '<div class="loading-dots">' +
            '<div class="dots"><div class="dot"></div><div class="dot"></div><div class="dot"></div></div>' +
            '<span class="loading-text">Đang tìm kiếm tài liệu...</span>' +
            '</div>';

        contentDiv.appendChild(bubble);
        row.appendChild(avatar);
        row.appendChild(contentDiv);
        messagesContainer.appendChild(row);
    }

    function removeLoading(id) {
        var el = document.getElementById(id);
        if (el) el.remove();
    }

    function scrollToBottom() {
        if (messagesContainer) {
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }
    }

    function escapeHtml(text) {
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Lightweight markdown renderer for AI responses
    function renderMarkdown(text) {
        if (!text) return '';
        var html = escapeHtml(text);

        // Code blocks (```...```)
        html = html.replace(/```([\s\S]*?)```/g, function(_, code) {
            return '<pre class="md-code-block"><code>' + code.trim() + '</code></pre>';
        });

        // Inline code (`...`)
        html = html.replace(/`([^`]+)`/g, '<code class="md-inline-code">$1</code>');

        // Bold (**text**)
        html = html.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');

        // Italic (*text*)
        html = html.replace(/(?<!\*)\*([^*]+)\*(?!\*)/g, '<em>$1</em>');

        // Convert lines to structured HTML
        var lines = html.split('\n');
        var result = [];
        var inList = false;

        for (var i = 0; i < lines.length; i++) {
            var line = lines[i];
            var trimmed = line.trim();

            // Bullet list items (- item or * item)
            if (/^[-*]\s+/.test(trimmed)) {
                if (!inList) {
                    result.push('<ul class="md-list">');
                    inList = true;
                }
                result.push('<li>' + trimmed.replace(/^[-*]\s+/, '') + '</li>');
            }
            // Numbered list items (1. item)
            else if (/^\d+\.\s+/.test(trimmed)) {
                if (!inList) {
                    result.push('<ol class="md-list">');
                    inList = true;
                }
                result.push('<li>' + trimmed.replace(/^\d+\.\s+/, '') + '</li>');
            }
            else {
                if (inList) {
                    // Close the list - detect if it was ul or ol
                    var lastListOpen = '';
                    for (var j = result.length - 1; j >= 0; j--) {
                        if (result[j] === '<ul class="md-list">') { lastListOpen = '</ul>'; break; }
                        if (result[j] === '<ol class="md-list">') { lastListOpen = '</ol>'; break; }
                    }
                    result.push(lastListOpen || '</ul>');
                    inList = false;
                }
                if (trimmed === '') {
                    result.push('<br/>');
                } else {
                    result.push('<p class="md-paragraph">' + line + '</p>');
                }
            }
        }

        if (inList) {
            var lastOpen = '';
            for (var k = result.length - 1; k >= 0; k--) {
                if (result[k] === '<ul class="md-list">') { lastOpen = '</ul>'; break; }
                if (result[k] === '<ol class="md-list">') { lastOpen = '</ol>'; break; }
            }
            result.push(lastOpen || '</ul>');
        }

        return result.join('');
    }

    // Initial scroll
    scrollToBottom();
    updateSendBtn();
})();
