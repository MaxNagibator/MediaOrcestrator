var __commentsData = {{data}};

(function () {
    var data = __commentsData || { groups: [], search: "" };
    var root = document.getElementById("root");

    var TOKEN_RE = /\[(id|club|public)(\d+)\|([^\]]+)\]|https?:\/\/[^\s<>"']+/g;
    var TRAIL_RE = /[)\].,!?;:]+$/;

    function el(tag, className) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        return node;
    }

    function text(value) {
        return document.createTextNode(value == null ? "" : String(value));
    }

    function tokenize(value) {
        var tokens = [];
        var last = 0;
        var m;
        TOKEN_RE.lastIndex = 0;
        while ((m = TOKEN_RE.exec(value)) !== null) {
            if (m.index > last) {
                tokens.push({ kind: "text", value: value.substring(last, m.index) });
            }
            if (m[1]) {
                var mentionUrl = (m[1] === "id" ? "https://vk.com/id" : "https://vk.com/" + m[1]) + m[2];
                var mentionToken = { kind: "mention" };
                mentionToken.url = mentionUrl;
                mentionToken.name = m[3];
                tokens.push(mentionToken);
            } else {
                var rawText = m[0];
                var trailing = "";
                var t = rawText.match(TRAIL_RE);
                if (t) {
                    trailing = t[0];
                    rawText = rawText.substring(0, rawText.length - trailing.length);
                }
                var urlToken = { kind: "url" };
                urlToken.url = rawText;
                urlToken.trail = trailing;
                tokens.push(urlToken);
            }
            last = m.index + m[0].length;
            if (m[0].length === 0) TOKEN_RE.lastIndex++;
        }
        if (last < value.length) {
            tokens.push({ kind: "text", value: value.substring(last) });
        }
        return tokens;
    }

    function appendHighlightedText(parent, value, search) {
        if (!value) return;
        if (!search) {
            parent.appendChild(text(value));
            return;
        }
        var safe = search.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
        var re = new RegExp(safe, "gi");
        var last = 0;
        var m;
        while ((m = re.exec(value)) !== null) {
            if (m.index > last) parent.appendChild(text(value.substring(last, m.index)));
            var mark = el("mark");
            mark.appendChild(text(m[0]));
            parent.appendChild(mark);
            last = m.index + m[0].length;
            if (m[0].length === 0) re.lastIndex++;
        }
        if (last < value.length) parent.appendChild(text(value.substring(last)));
    }

    function appendLinkified(parent, value, search) {
        if (!value) return;
        var tokens = tokenize(value);
        for (var i = 0; i < tokens.length; i++) {
            var tok = tokens[i];
            if (tok.kind === "text") {
                appendHighlightedText(parent, tok.value, search);
            } else if (tok.kind === "mention") {
                var link = el("a", "mention");
                link.setAttribute("href", tok.url);
                link.setAttribute("target", "_blank");
                link.setAttribute("rel", "noopener");
                link.appendChild(text("@" + tok.name));
                parent.appendChild(link);
            } else if (tok.kind === "url") {
                var urlLink = el("a");
                urlLink.setAttribute("href", tok.url);
                urlLink.setAttribute("target", "_blank");
                urlLink.setAttribute("rel", "noopener");
                urlLink.appendChild(text(tok.url));
                parent.appendChild(urlLink);
                if (tok.trail) parent.appendChild(text(tok.trail));
            }
        }
    }

    function buildIndex(comments) {
        var byId = {};
        var childrenByParent = {};
        for (var i = 0; i < comments.length; i++) byId[comments[i].id] = comments[i];
        for (var j = 0; j < comments.length; j++) {
            var c = comments[j];
            if (c.parent && byId[c.parent]) {
                if (!childrenByParent[c.parent]) childrenByParent[c.parent] = [];
                childrenByParent[c.parent].push(c);
            }
        }
        var index = {};
        index.byId = byId;
        index.childrenByParent = childrenByParent;
        return index;
    }

    function initialOf(name) {
        if (!name) return "?";
        var trimmed = name.replace(/^\s+/, "");
        if (!trimmed) return "?";
        return trimmed.charAt(0);
    }

    function paletteIndex(name) {
        var s = name || "";
        var h = 0;
        for (var i = 0; i < s.length; i++) h = h * 31 + s.charCodeAt(i) & 0x7fffffff;
        return h % 6 + 1;
    }

    function notify(kind, sourceId, externalMediaId, externalCommentId, parentExternalCommentId, payload, authorId) {
        try {
            if (authorId) {
                window.external.OnActionAs(
                    kind,
                    sourceId || "",
                    externalMediaId || "",
                    externalCommentId || "",
                    parentExternalCommentId || "",
                    payload || "",
                    authorId);
            } else {
                window.external.OnAction(
                    kind,
                    sourceId || "",
                    externalMediaId || "",
                    externalCommentId || "",
                    parentExternalCommentId || "",
                    payload || "");
            }
        } catch (err) {
            alert("Не удалось отправить запрос: " + err);
        }
    }

    function loadAuthors(sourceId, externalMediaId) {
        try {
            if (!window.external) return [];
            var raw = window.external.GetAuthors(sourceId || "", externalMediaId || "");
            if (!raw) return [];
            var parsed = JSON.parse(String(raw));
            return Array.isArray(parsed) ? parsed : [];
        } catch (err) {
            return [];
        }
    }

    var __authorSubscribers = {};

    function authorKey(sourceId, externalMediaId) {
        return (sourceId || "") + "|" + (externalMediaId || "");
    }

    function subscribeAuthors(sourceId, externalMediaId, callback) {
        var key = authorKey(sourceId, externalMediaId);
        if (!__authorSubscribers[key]) __authorSubscribers[key] = [];
        __authorSubscribers[key].push(callback);
        return function () {
            var list = __authorSubscribers[key];
            if (!list) return;
            for (var i = 0; i < list.length; i++) {
                if (list[i] === callback) {
                    list.splice(i, 1);
                    return;
                }
            }
        };
    }

    window.__setAuthors = function (sourceId, externalMediaId, json) {
        try {
            var list = __authorSubscribers[authorKey(sourceId, externalMediaId)];
            if (!list || list.length === 0) return true;

            var parsed = json ? JSON.parse(String(json)) : [];
            if (!Array.isArray(parsed)) parsed = [];

            var snapshot = list.slice();
            for (var i = 0; i < snapshot.length; i++) {
                try {
                    snapshot[i](parsed);
                } catch (e) {
                }
            }
            return true;
        } catch (e) {
            return false;
        }
    };

    function buildAuthorAvatar(author, baseClass) {
        var name = author && author.name ? author.name : "";
        var avatar = el("span", baseClass + " placeholder-" + paletteIndex(name));
        avatar.appendChild(text(initialOf(name)));
        if (author && author.avatar) {
            var img = document.createElement("img");
            img.setAttribute("alt", "");
            img.onload = function () {
                while (avatar.firstChild) avatar.removeChild(avatar.firstChild);
                avatar.appendChild(img);
            };
            img.onerror = function () {
            };
            img.setAttribute("src", author.avatar);
        }
        return avatar;
    }

    function buildAuthorRow(author, baseClass) {
        var row = el("span", "author-select-row");
        row.appendChild(buildAuthorAvatar(author, baseClass));
        var name = el("span", "author-select-name");
        name.appendChild(text(author && author.name || author && author.id || ""));
        row.appendChild(name);
        return row;
    }

    var __activeAuthorSelect = null;
    var __authorSelectGlobalBound = false;

    function closeActiveAuthorSelect() {
        if (!__activeAuthorSelect) return;
        var cls = __activeAuthorSelect.className || "";
        __activeAuthorSelect.className = cls.replace(/\s*\bopen\b/g, "");
        __activeAuthorSelect = null;
    }

    function isInsideNode(target, node) {
        var n = target;
        while (n) {
            if (n === node) return true;
            n = n.parentNode;
        }
        return false;
    }

    function ensureAuthorSelectGlobalHandlers() {
        if (__authorSelectGlobalBound) return;
        __authorSelectGlobalBound = true;
        if (document.addEventListener) {
            document.addEventListener("mousedown", function (ev) {
                if (!__activeAuthorSelect) return;
                var target = ev.target || ev.srcElement;
                if (!isInsideNode(target, __activeAuthorSelect)) {
                    closeActiveAuthorSelect();
                }
            }, true);
            document.addEventListener("keydown", function (ev) {
                if (!__activeAuthorSelect) return;
                if (ev.keyCode === 27) {
                    closeActiveAuthorSelect();
                }
            }, true);
        }
    }

    function buildAuthorSelect(sourceId, externalMediaId) {
        var authors = loadAuthors(sourceId, externalMediaId);

        ensureAuthorSelectGlobalHandlers();

        var wrap = el("div", "author-select");

        var label = el("span", "author-select-label");
        label.appendChild(text("От имени:"));
        wrap.appendChild(label);

        var trigger = el("span", "author-select-trigger");
        trigger.setAttribute("tabindex", "0");
        trigger.setAttribute("role", "button");
        trigger.setAttribute("aria-haspopup", "listbox");

        var triggerRowHost = el("span", "author-select-trigger-host");
        triggerRowHost.style.display = "inline-flex";
        triggerRowHost.style.alignItems = "center";
        triggerRowHost.style.minWidth = "0";
        triggerRowHost.style.flex = "1 1 auto";
        trigger.appendChild(triggerRowHost);

        var arrow = el("span", "arrow");
        arrow.appendChild(text("▾"));
        trigger.appendChild(arrow);

        wrap.appendChild(trigger);

        var menu = el("div", "author-select-menu");
        wrap.appendChild(menu);

        var selectedId = "";
        var byId = {};
        var defaultId = "";

        function applyAuthors(list) {
            authors = Array.isArray(list) ? list : [];
            byId = {};
            defaultId = "";
            for (var i = 0; i < authors.length; i++) {
                var a = authors[i];
                if (!a) continue;
                byId[a.id] = a;
                if (a.isDefault && !defaultId) defaultId = a.id;
            }
            if (!defaultId && authors.length > 0) defaultId = authors[0].id;
            if (!selectedId || !byId[selectedId]) selectedId = defaultId;
        }

        applyAuthors(authors);

        function renderTrigger() {
            while (triggerRowHost.firstChild) triggerRowHost.removeChild(triggerRowHost.firstChild);
            if (authors.length === 0) {
                triggerRowHost.appendChild(text("Загрузка..."));
                return;
            }
            var current = byId[selectedId] || authors[0];
            triggerRowHost.appendChild(buildAuthorRow(current, "author-select-avatar"));
        }

        function renderMenu() {
            while (menu.firstChild) menu.removeChild(menu.firstChild);
            for (var k = 0; k < authors.length; k++) {
                (function (author) {
                    var opt = el("div", "author-select-option" + (author.id === selectedId ? " active" : ""));
                    opt.setAttribute("role", "option");
                    opt.appendChild(buildAuthorRow(author, "author-select-avatar"));
                    opt.onclick = function (ev) {
                        if (ev && ev.stopPropagation) ev.stopPropagation();
                        selectedId = author.id;
                        renderTrigger();
                        renderMenu();
                        closeActiveAuthorSelect();
                    };
                    menu.appendChild(opt);
                })(authors[k]);
            }
        }

        var unsubscribe = subscribeAuthors(sourceId, externalMediaId, function (list) {
            applyAuthors(list);
            renderTrigger();
            renderMenu();
        });
        wrap.__unsubscribeAuthors = unsubscribe;

        function openMenu() {
            if (__activeAuthorSelect && __activeAuthorSelect !== wrap) {
                closeActiveAuthorSelect();
            }
            if (wrap.className.indexOf("open") < 0) {
                wrap.className = "author-select open";
            }
            __activeAuthorSelect = wrap;
        }

        function toggleMenu() {
            if (__activeAuthorSelect === wrap) {
                closeActiveAuthorSelect();
            } else {
                openMenu();
            }
        }

        trigger.onclick = function (ev) {
            if (ev && ev.stopPropagation) ev.stopPropagation();
            toggleMenu();
        };

        trigger.onkeydown = function (ev) {
            var key = ev.keyCode;
            if (key === 13 || key === 32 || key === 40) {
                if (ev.preventDefault) ev.preventDefault();
                openMenu();
            } else if (key === 27) {
                closeActiveAuthorSelect();
            }
        };

        renderTrigger();
        renderMenu();

        wrap.__getAuthorId = function () {
            return selectedId || "";
        };
        return wrap;
    }

    function buildForm(initialText, placeholder, submitLabel, onSubmit, onCancel) {
        var form = el("div", "edit-form");
        var area = el("textarea");
        area.value = initialText || "";
        if (placeholder) area.setAttribute("placeholder", placeholder);
        form.appendChild(area);

        var caret = (initialText || "").length;

        var actions = el("div", "form-actions");

        var submitBtn = el("button");
        submitBtn.appendChild(text(submitLabel));
        actions.appendChild(submitBtn);

        var cancelBtn = el("button", "secondary");
        cancelBtn.appendChild(text("Отмена"));
        actions.appendChild(cancelBtn);

        form.appendChild(actions);

        submitBtn.onclick = function () {
            var value = area.value;
            if (!value || !value.replace(/\s+/g, "")) {
                area.focus();
                return;
            }
            submitBtn.setAttribute("disabled", "disabled");
            cancelBtn.setAttribute("disabled", "disabled");
            onSubmit(value);
        };

        cancelBtn.onclick = function () {
            onCancel();
        };

        setTimeout(function () {
            area.focus();
            try {
                if (typeof area.setSelectionRange === "function") {
                    area.setSelectionRange(caret, caret);
                } else if (area.createTextRange) {
                    var range = area.createTextRange();
                    range.collapse(true);
                    range.moveEnd("character", caret);
                    range.moveStart("character", caret);
                    range.select();
                }
            } catch (err) {
            }
        }, 0);
        return form;
    }

    function replyPrefix(author) {
        var name = (author || "").replace(/^\s+|\s+$/g, "");
        return name ? name + ", " : "";
    }

    function renderActions(c, bodyHost, formHost) {
        if (!c.hasMutations && !c.hasLikes) return null;

        var bar = el("div", "actions");

        if (c.hasLikes && !c.deleted) {
            var likeBtn = el("button", "like-btn" + (c.likedByMe ? " liked" : ""));
            var label = c.likedByMe ? "♥ Не нравится" : "♡ Нравится";
            likeBtn.appendChild(text(label));
            likeBtn.onclick = function () {
                notify(c.likedByMe ? "Unlike" : "Like",
                    c.sourceId, c.externalMediaId, c.externalCommentId, "", "");
            };
            bar.appendChild(likeBtn);
        }

        if (!c.hasMutations) {
            return bar.childNodes.length ? bar : null;
        }

        if (!c.deleted) {
            var replyBtn = el("button");
            replyBtn.appendChild(text("Ответить"));
            replyBtn.onclick = function () {
                while (formHost.firstChild) formHost.removeChild(formHost.firstChild);
                var authorSelect = c.hasAuthors ? buildAuthorSelect(c.sourceId, c.externalMediaId) : null;
                var form = buildForm(replyPrefix(c.author), "Ответ для " + (c.author || ""), "Отправить",
                    function (value) {
                        var authorId = authorSelect && authorSelect.__getAuthorId ? authorSelect.__getAuthorId() : "";
                        notify("Create", c.sourceId, c.externalMediaId, "", c.externalCommentId, value, authorId);
                    },
                    function () {
                        while (formHost.firstChild) formHost.removeChild(formHost.firstChild);
                    });
                if (authorSelect) form.insertBefore(authorSelect, form.firstChild);
                formHost.appendChild(form);
            };
            bar.appendChild(replyBtn);
        }

        if (c.canEdit && !c.deleted) {
            var editBtn = el("button");
            editBtn.appendChild(text("Изменить"));
            editBtn.onclick = function () {
                var originalText = bodyHost.__originalText || "";
                bodyHost.style.display = "none";
                while (formHost.firstChild) formHost.removeChild(formHost.firstChild);
                var form = buildForm(originalText, "", "Сохранить",
                    function (value) {
                        notify("Edit", c.sourceId, c.externalMediaId, c.externalCommentId, "", value);
                    },
                    function () {
                        while (formHost.firstChild) formHost.removeChild(formHost.firstChild);
                        bodyHost.style.display = "";
                    });
                formHost.appendChild(form);
            };
            bar.appendChild(editBtn);
        }

        if (c.canDelete && !c.deleted) {
            var deleteBtn = el("button");
            deleteBtn.appendChild(text("Удалить"));
            deleteBtn.onclick = function () {
                notify("Delete", c.sourceId, c.externalMediaId, c.externalCommentId, "", "");
            };
            bar.appendChild(deleteBtn);
        }

        if (c.canDelete && c.deleted) {
            var restoreBtn = el("button");
            restoreBtn.appendChild(text("Восстановить"));
            restoreBtn.onclick = function () {
                notify("Restore", c.sourceId, c.externalMediaId, c.externalCommentId, "", "");
            };
            bar.appendChild(restoreBtn);
        }

        return bar.childNodes.length ? bar : null;
    }

    function renderCompose(g) {
        var mutating = [];
        var sources = g.sources || [];
        for (var i = 0; i < sources.length; i++) {
            if (sources[i].hasMutations) mutating.push(sources[i]);
        }
        if (mutating.length === 0) return null;

        var box = el("div", "compose");
        var formHost = el("div", "form-host");
        box.appendChild(formHost);

        var triggerBar = el("div", "actions");
        var triggerBtn = el("button");
        triggerBtn.appendChild(text("Написать комментарий"));
        triggerBar.appendChild(triggerBtn);
        box.appendChild(triggerBar);

        triggerBtn.onclick = function () {
            triggerBar.style.display = "none";
            while (formHost.firstChild) formHost.removeChild(formHost.firstChild);

            var area = el("textarea");
            area.setAttribute("placeholder", "Новый комментарий...");
            formHost.appendChild(area);

            var actions = el("div", "form-actions");

            var sourceSelect = null;
            if (mutating.length > 1) {
                sourceSelect = el("select");
                for (var s = 0; s < mutating.length; s++) {
                    var opt = el("option");
                    opt.value = mutating[s].sourceId + "|" + mutating[s].externalId;
                    opt.appendChild(text(mutating[s].sourceTitle));
                    sourceSelect.appendChild(opt);
                }
                actions.appendChild(sourceSelect);
            }

            function pickedSource() {
                if (!sourceSelect) return mutating[0];
                var parts = sourceSelect.value.split("|");
                for (var k = 0; k < mutating.length; k++) {
                    if (mutating[k].sourceId === parts[0] && mutating[k].externalId === parts[1]) {
                        return mutating[k];
                    }
                }
                return mutating[0];
            }

            var authorWrap = el("div", "author-select-host");
            formHost.insertBefore(authorWrap, area);

            var currentAuthorSelect = null;

            function refreshAuthorSelect() {
                while (authorWrap.firstChild) authorWrap.removeChild(authorWrap.firstChild);
                currentAuthorSelect = null;
                var picked = pickedSource();
                if (!picked || !picked.hasAuthors) return;
                var sel = buildAuthorSelect(picked.sourceId, picked.externalId);
                if (sel) {
                    authorWrap.appendChild(sel);
                    currentAuthorSelect = sel;
                }
            }

            refreshAuthorSelect();
            if (sourceSelect) sourceSelect.onchange = refreshAuthorSelect;

            var submitBtn = el("button");
            submitBtn.appendChild(text("Отправить"));
            actions.appendChild(submitBtn);

            var cancelBtn = el("button", "secondary");
            cancelBtn.appendChild(text("Отмена"));
            actions.appendChild(cancelBtn);

            formHost.appendChild(actions);

            submitBtn.onclick = function () {
                var value = area.value;
                if (!value || !value.replace(/\s+/g, "")) {
                    area.focus();
                    return;
                }

                var picked = pickedSource();
                var authorId = currentAuthorSelect && currentAuthorSelect.__getAuthorId ? currentAuthorSelect.__getAuthorId() : "";

                submitBtn.setAttribute("disabled", "disabled");
                cancelBtn.setAttribute("disabled", "disabled");
                notify("Create", picked.sourceId, picked.externalId, "", "", value, authorId);
            };

            cancelBtn.onclick = function () {
                while (formHost.firstChild) formHost.removeChild(formHost.firstChild);
                triggerBar.style.display = "";
            };

            setTimeout(function () {
                area.focus();
            }, 0);
        };

        return box;
    }

    function renderAvatar(c) {
        var avatar = el("div", "avatar placeholder-" + paletteIndex(c.author));
        avatar.appendChild(text(initialOf(c.author)));

        if (c.avatar) {
            var img = document.createElement("img");
            img.setAttribute("alt", "");
            img.onload = function () {
                while (avatar.firstChild) avatar.removeChild(avatar.firstChild);
                avatar.appendChild(img);
            };
            img.onerror = function () {
            };
            img.setAttribute("src", c.avatar);
        }

        return avatar;
    }

    function renderComment(c, childrenByParent, showSource) {
        var div = el("div", "comment" + (c.deleted ? " deleted" : ""));
        div._commentId = c.id;

        div.appendChild(renderAvatar(c));

        var head = el("div", "head");
        var author = el("span", "author");
        author.appendChild(text(c.author || "—"));
        head.appendChild(author);

        if (c.isAuthor) {
            var badge = el("span", "author-badge");
            badge.setAttribute("title", "Комментарий от автора");
            badge.appendChild(text("Автор"));
            head.appendChild(badge);
        }

        var date = el("span", "date");
        date.appendChild(text(c.date));
        head.appendChild(date);

        if (c.hasPermalink) {
            var permalink = el("a", "permalink");
            permalink.setAttribute("href",
                "app://comment/"
                + encodeURIComponent(c.sourceId)
                + "/" + encodeURIComponent(c.externalMediaId)
                + "/" + encodeURIComponent(c.externalCommentId));
            permalink.setAttribute("title", "Открыть комментарий в источнике");
            permalink.appendChild(text("перейти"));
            head.appendChild(permalink);
        }

        if (c.likes > 0) {
            var likes = el("span", "likes");
            likes.appendChild(text("♥ " + c.likes));
            head.appendChild(likes);
        }

        if (c.likedByAuthor) {
            var heart = el("span", "liked-by-author");
            heart.setAttribute("title", "Лайк от автора");
            heart.appendChild(text("♥ автор"));
            head.appendChild(heart);
        }

        if (showSource && c.source) {
            var src = el("span", "source-badge");
            src.appendChild(text(c.source));
            head.appendChild(src);
        }

        div.appendChild(head);

        var body = el("div", "body");
        if (c.deleted) {
            body.appendChild(text("[удалён]"));
        } else {
            appendLinkified(body, c.text || "", data.search);
        }
        body.__originalText = c.text || "";
        div.appendChild(body);

        var formHost = el("div", "form-host");
        div.appendChild(formHost);

        div._data = c;

        var actionsBar = renderActions(c, body, formHost);
        if (actionsBar) div.appendChild(actionsBar);

        var children = childrenByParent[c.id];
        if (children && children.length) {
            var replies = el("div", "replies");
            for (var i = 0; i < children.length; i++) {
                replies.appendChild(renderComment(children[i], childrenByParent, showSource));
            }
            div.appendChild(replies);
        }

        return div;
    }

    function renderGroup(g) {
        var media = el("div", "media");
        media._groupKey = g.key;

        var head = el("div", "media-head");

        var actions = el("div", "media-actions");
        var sources = g.sources || [];
        var multipleSources = sources.length > 1;
        media._multipleSources = multipleSources;
        for (var si = 0; si < sources.length; si++) {
            var s = sources[si];
            if (s.hasExternalLink) {
                var open = el("a");
                open.setAttribute("href",
                    "app://open/" + encodeURIComponent(s.sourceId) + "/" + encodeURIComponent(s.externalId));
                open.setAttribute("title", "Открыть медиа в источнике " + s.sourceTitle);
                open.appendChild(text(multipleSources ? "открыть " + s.sourceTitle : "открыть"));
                actions.appendChild(open);
            }
            var refresh = el("a");
            refresh.setAttribute("href",
                "app://fetch/" + encodeURIComponent(s.sourceId) + "/" + encodeURIComponent(s.externalId));
            refresh.setAttribute("title", "Перезагрузить комментарии из " + s.sourceTitle);
            refresh.appendChild(text(multipleSources ? "обновить " + s.sourceTitle : "обновить"));
            actions.appendChild(refresh);
        }
        head.appendChild(actions);

        var title = el("div", "media-title");
        if (g.mediaId) {
            var titleLink = el("a");
            titleLink.setAttribute("href", "app://media/" + encodeURIComponent(g.mediaId));
            titleLink.appendChild(text(g.mediaTitle));
            title.appendChild(titleLink);
        } else {
            title.appendChild(text(g.mediaTitle));
        }
        head.appendChild(title);

        var meta = el("div", "media-meta");
        var parts = [];
        for (var pi = 0; pi < sources.length; pi++) {
            parts.push(sources[pi].sourceTitle + ": " + sources[pi].count);
        }
        var metaText = parts.join("  ·  ");
        if (multipleSources) metaText += "  ·  всего: " + g.total;
        meta.appendChild(text(metaText));
        if (g.mediaMissing) meta.appendChild(text("  ·  локально не найдено"));
        head.appendChild(meta);

        media.appendChild(head);

        var compose = renderCompose(g);
        if (compose) media.appendChild(compose);

        var threads = el("div", "threads");
        var idx = buildIndex(g.comments);
        for (var i = 0; i < g.comments.length; i++) {
            var c = g.comments[i];
            if (!c.parent || !idx.byId[c.parent]) {
                threads.appendChild(renderComment(c, idx.childrenByParent, multipleSources));
            }
        }
        media.appendChild(threads);

        return media;
    }

    function clear(node) {
        while (node.firstChild) node.removeChild(node.firstChild);
    }

    function isFlat() {
        return (data.layout || "grouped") === "flat";
    }

    function relativeTime(iso) {
        if (!iso) return "";
        var t = Date.parse(iso);
        if (isNaN(t)) return "";
        var diff = Math.floor((Date.now() - t) / 1000);
        if (diff < 5) return "только что";
        if (diff < 60) return diff + " сек. назад";
        if (diff < 3600) return Math.floor(diff / 60) + " мин. назад";
        if (diff < 86400) return Math.floor(diff / 3600) + " ч. назад";
        if (diff < 2592000) return Math.floor(diff / 86400) + " дн. назад";
        if (diff < 31536000) return Math.floor(diff / 2592000) + " мес. назад";
        return Math.floor(diff / 31536000) + " г. назад";
    }

    function buildFlatRowMeta(comment, group) {
        var mediaSources = {};
        var srcs = group.sources || [];
        for (var si = 0; si < srcs.length; si++) {
            mediaSources[srcs[si].sourceId] = srcs[si];
        }
        return {
            comment: comment,
            group: group,
            sources: mediaSources,
            idx: buildIndex(group.comments || [])
        };
    }

    function collectFlatRows() {
        var rows = [];
        var indexes = {};
        var groups = data.groups || [];
        for (var gi = 0; gi < groups.length; gi++) {
            var g = groups[gi];
            if (!g || !g.comments) continue;
            var idx = buildIndex(g.comments);
            indexes[g.key] = idx;
            var mediaSources = {};
            var srcs = g.sources || [];
            for (var si = 0; si < srcs.length; si++) {
                mediaSources[srcs[si].sourceId] = srcs[si];
            }
            for (var ci = 0; ci < g.comments.length; ci++) {
                var c = g.comments[ci];
                if (c.parent && idx.byId[c.parent]) continue;
                rows.push({ comment: c, group: g, sources: mediaSources, idx: idx });
            }
        }
        var sort = data.sort || "newest";
        rows.sort(function (a, b) {
            if (sort === "mostlikes") {
                return (b.comment.likes || 0) - (a.comment.likes || 0);
            }
            var ta = Date.parse(a.comment.dateIso) || 0;
            var tb = Date.parse(b.comment.dateIso) || 0;
            return sort === "oldest" ? ta - tb : tb - ta;
        });
        return rows;
    }

    function renderFlatRow(row, depth) {
        var c = row.comment;
        var rowDiv = el("div", "flat-row" + (c.deleted ? " deleted" : ""));
        rowDiv._commentId = c.id;
        rowDiv._groupKey = row.group.key;

        rowDiv.appendChild(buildFlatAvatar(c));

        var main = el("div", "flat-main");

        var head = el("div", "flat-head");
        var chip = el("span", "flat-author-chip" + (c.isAuthor ? " is-author" : ""));
        if (c.isAuthor) chip.setAttribute("title", "Комментарий от автора");
        chip.appendChild(text(c.author ? "@" + c.author : "—"));
        head.appendChild(chip);

        if (c.likes > 0) {
            var likes = el("span", "flat-likes-small");
            likes.appendChild(text("  ♥ " + c.likes));
            head.appendChild(likes);
        }

        if (c.likedByAuthor) {
            var heart = el("span", "liked-by-author");
            heart.setAttribute("title", "Лайк от автора");
            heart.appendChild(text("♥ автор"));
            head.appendChild(heart);
        }

        var meta = el("span", "flat-meta");
        meta.appendChild(text("· " + relativeTime(c.dateIso) + " · " + (c.source || "")));
        head.appendChild(meta);

        if (c.hasPermalink) {
            var permalink = el("a", "permalink");
            permalink.setAttribute("href",
                "app://comment/"
                + encodeURIComponent(c.sourceId)
                + "/" + encodeURIComponent(c.externalMediaId)
                + "/" + encodeURIComponent(c.externalCommentId));
            permalink.setAttribute("title", "Открыть комментарий в источнике");
            permalink.appendChild(text("перейти"));
            head.appendChild(permalink);
        }

        main.appendChild(head);

        var body = el("div", "flat-body");
        if (c.deleted) {
            body.appendChild(text("[удалён]"));
        } else {
            appendLinkified(body, c.text || "", data.search);
        }
        body.__originalText = c.text || "";
        main.appendChild(body);

        var formHost = el("div", "flat-form-host");
        main.appendChild(formHost);

        var actionsBar = buildFlatActions(c, body, formHost, row, rowDiv, depth);
        if (actionsBar) main.appendChild(actionsBar);

        rowDiv._data = c;
        rowDiv._row = row;
        rowDiv._depth = depth;

        rowDiv.appendChild(main);

        if (depth === 0) {
            rowDiv.appendChild(buildFlatThumb(row));
        }

        return rowDiv;
    }

    function buildFlatAvatar(c) {
        var avatar = el("div", "flat-avatar placeholder-" + paletteIndex(c.author));
        avatar.appendChild(text(initialOf(c.author)));
        if (c.avatar) {
            var img = document.createElement("img");
            img.setAttribute("alt", "");
            img.onload = function () {
                while (avatar.firstChild) avatar.removeChild(avatar.firstChild);
                avatar.appendChild(img);
            };
            img.onerror = function () {
            };
            img.setAttribute("src", c.avatar);
        }
        return avatar;
    }

    function buildFlatThumb(row) {
        var c = row.comment;
        var wrap = el("div", "flat-thumb");

        var thumbBox = el("div", "flat-thumb-img");
        if (c.mediaThumbnailUrl) {
            var img = document.createElement("img");
            img.setAttribute("alt", "");
            img.setAttribute("src", c.mediaThumbnailUrl);
            thumbBox.appendChild(img);
        }
        if (c.hasMediaExternalLink) {
            var openLink = el("a", "flat-thumb-link");
            openLink.setAttribute("href",
                "app://open/" + encodeURIComponent(c.sourceId) + "/" + encodeURIComponent(c.externalMediaId));
            openLink.setAttribute("title", "Открыть медиа в источнике");
            openLink.appendChild(text("↗"));
            thumbBox.appendChild(openLink);
        }
        wrap.appendChild(thumbBox);

        var title = el("a", "flat-thumb-title");
        if (row.group.mediaId) {
            title.setAttribute("href", "app://media/" + encodeURIComponent(row.group.mediaId));
        } else {
            title.setAttribute("href", "#");
            title.onclick = function (e) {
                if (e && e.preventDefault) e.preventDefault();
            };
        }
        title.appendChild(text(c.mediaTitle || row.group.mediaTitle || ""));
        wrap.appendChild(title);

        return wrap;
    }

    function buildLikeButton(c) {
        var likeBtn = el("button", "like-btn" + (c.likedByMe ? " liked" : ""));
        likeBtn.setAttribute("title", c.likedByMe ? "Убрать лайк" : "Поставить лайк");
        likeBtn.appendChild(text(c.likedByMe ? "♥" : "♡"));
        likeBtn.onclick = function () {
            notify(c.likedByMe ? "Unlike" : "Like",
                c.sourceId, c.externalMediaId, c.externalCommentId, "", "");
        };
        return likeBtn;
    }

    function buildFlatActions(c, bodyHost, formHost, row, rowDiv, depth) {
        var bar = el("div", "flat-actions");
        var hasAny = false;

        if (c.hasMutations && !c.deleted) {
            var replyBtn = el("button");
            replyBtn.appendChild(text("Ответить"));
            replyBtn.onclick = function () {
                while (formHost.firstChild) formHost.removeChild(formHost.firstChild);
                var authorSelect = c.hasAuthors ? buildAuthorSelect(c.sourceId, c.externalMediaId) : null;
                var form = buildForm(replyPrefix(c.author), "Ответ для " + (c.author || ""), "Отправить",
                    function (value) {
                        var authorId = authorSelect && authorSelect.__getAuthorId ? authorSelect.__getAuthorId() : "";
                        notify("Create", c.sourceId, c.externalMediaId, "", c.externalCommentId, value, authorId);
                    },
                    function () {
                        while (formHost.firstChild) formHost.removeChild(formHost.firstChild);
                    });
                if (authorSelect) form.insertBefore(authorSelect, form.firstChild);
                formHost.appendChild(form);
            };
            bar.appendChild(replyBtn);
            hasAny = true;
        }

        if (c.hasLikes && !c.deleted) {
            bar.appendChild(buildLikeButton(c));
            var likeCount = el("span", "flat-like-count");
            if (c.likes > 0) likeCount.appendChild(text(String(c.likes)));
            bar.appendChild(likeCount);
            hasAny = true;
        }

        if (c.canEdit && !c.deleted) {
            var editBtn = el("button", "flat-action-secondary");
            editBtn.appendChild(text("Изменить"));
            editBtn.onclick = function () {
                var originalText = bodyHost.__originalText || "";
                bodyHost.style.display = "none";
                while (formHost.firstChild) formHost.removeChild(formHost.firstChild);
                var form = buildForm(originalText, "", "Сохранить",
                    function (value) {
                        notify("Edit", c.sourceId, c.externalMediaId, c.externalCommentId, "", value);
                    },
                    function () {
                        while (formHost.firstChild) formHost.removeChild(formHost.firstChild);
                        bodyHost.style.display = "";
                    });
                formHost.appendChild(form);
            };
            bar.appendChild(editBtn);
            hasAny = true;
        }

        if (c.canDelete && !c.deleted) {
            var delBtn = el("button", "flat-action-secondary");
            delBtn.appendChild(text("Удалить"));
            delBtn.onclick = function () {
                notify("Delete", c.sourceId, c.externalMediaId, c.externalCommentId, "", "");
            };
            bar.appendChild(delBtn);
            hasAny = true;
        }

        if (c.canDelete && c.deleted) {
            var restoreBtn = el("button");
            restoreBtn.appendChild(text("Восстановить"));
            restoreBtn.onclick = function () {
                notify("Restore", c.sourceId, c.externalMediaId, c.externalCommentId, "", "");
            };
            bar.appendChild(restoreBtn);
            hasAny = true;
        }

        var isRoot = (depth || 0) === 0;
        var hasReplies = c.replyCount > 0;
        if (isRoot || hasReplies) {
            var toggle = el("button", "toggle-replies" + (hasReplies ? "" : " empty"));
            var label = hasReplies
                ? c.replyCount + " " + replyWord(c.replyCount)
                : "Нет ответов";
            toggle.appendChild(text((hasReplies ? "▾ " : "") + label));
            var expanded = false;
            var repliesHost = null;
            function doExpand() {
                if (expanded || !hasReplies) return;
                expanded = true;
                while (toggle.firstChild) toggle.removeChild(toggle.firstChild);
                toggle.appendChild(text("▴ Скрыть ответы"));
                repliesHost = renderFlatReplies(c, row);
                bar.parentNode.appendChild(repliesHost);
                var childNodes = repliesHost.childNodes;
                for (var ci = 0; ci < childNodes.length; ci++) {
                    var childRow = childNodes[ci];
                    if (childRow && childRow.__expandReplies) {
                        childRow.__expandReplies();
                    }
                }
            }
            function doCollapse() {
                if (!expanded) return;
                expanded = false;
                while (toggle.firstChild) toggle.removeChild(toggle.firstChild);
                toggle.appendChild(text("▾ " + label));
                if (repliesHost && repliesHost.parentNode) {
                    repliesHost.parentNode.removeChild(repliesHost);
                }
                repliesHost = null;
            }
            if (hasReplies) {
                toggle.onclick = function () {
                    if (expanded) doCollapse(); else doExpand();
                };
            }
            if (rowDiv) {
                rowDiv.__expandReplies = doExpand;
                rowDiv.__hasExpandedReplies = function () { return expanded; };
                rowDiv.__getRepliesHost = function () { return repliesHost; };
            }
            bar.appendChild(toggle);
            hasAny = true;
        }

        return hasAny ? bar : null;
    }

    function replyWord(n) {
        var mod10 = n % 10;
        var mod100 = n % 100;
        if (mod10 === 1 && mod100 !== 11) return "ответ";
        if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) return "ответа";
        return "ответов";
    }

    function renderFlatReplies(parentComment, parentRow) {
        var host = el("div", "flat-replies");
        var children = parentRow.idx.childrenByParent[parentComment.id] || [];
        for (var i = 0; i < children.length; i++) {
            var childRow = {
                comment: children[i],
                group: parentRow.group,
                sources: parentRow.sources,
                idx: parentRow.idx
            };
            host.appendChild(renderFlatRow(childRow, 1));
        }
        return host;
    }

    function renderFlat() {
        clear(root);
        var rows = collectFlatRows();
        if (rows.length === 0) {
            var empty = el("div", "flat-empty");
            empty.appendChild(text("Ничего не найдено"));
            root.appendChild(empty);
            return;
        }
        var list = el("div", "flat-list");
        for (var i = 0; i < rows.length; i++) {
            list.appendChild(renderFlatRow(rows[i], 0));
        }
        root.appendChild(list);
    }

    function render() {
        if (isFlat()) {
            renderFlat();
            return;
        }
        clear(root);
        if (!data.groups || data.groups.length === 0) {
            var empty = el("div", "empty");
            empty.appendChild(text("Ничего не найдено"));
            root.appendChild(empty);
            return;
        }
        var frag = document.createDocumentFragment();
        for (var i = 0; i < data.groups.length; i++) {
            frag.appendChild(renderGroup(data.groups[i]));
        }
        root.appendChild(frag);
    }

    render();

    function findCommentDiv(id) {
        var nodes = document.getElementsByClassName("comment");
        for (var i = 0; i < nodes.length; i++) {
            if (nodes[i]._commentId === id) return nodes[i];
        }
        return null;
    }

    function findFlatRow(id) {
        var nodes = document.getElementsByClassName("flat-row");
        for (var i = 0; i < nodes.length; i++) {
            if (nodes[i]._commentId === id) return nodes[i];
        }
        return null;
    }

    function findDescendantByClass(parent, cls) {
        if (!parent) return null;
        var nodes = parent.getElementsByClassName(cls);
        return nodes && nodes.length ? nodes[0] : null;
    }

    function rebuildFlatActions(rowDiv) {
        var c = rowDiv._data;
        if (!c) return;
        var main = findDescendantByClass(rowDiv, "flat-main");
        if (!main) return;
        var body = findDescendantByClass(main, "flat-body");
        var formHost = findDescendantByClass(main, "flat-form-host");
        if (!body || !formHost) return;

        var existing = findDescendantByClass(main, "flat-actions");
        if (existing && existing.parentNode === main) {
            main.removeChild(existing);
        }

        var fresh = buildFlatActions(c, body, formHost, rowDiv._row, rowDiv, rowDiv._depth || 0);
        if (fresh) main.appendChild(fresh);
    }

    function updateFlatLikeCount(rowDiv, count) {
        var bar = findDescendantByClass(rowDiv, "flat-actions");
        if (!bar) return;
        var counter = findDescendantByClass(bar, "flat-like-count");
        if (!counter) return;
        while (counter.firstChild) counter.removeChild(counter.firstChild);
        if (count > 0) counter.appendChild(text(String(count)));
    }

    function updateFlatLikeButton(rowDiv, liked) {
        var bar = findDescendantByClass(rowDiv, "flat-actions");
        if (!bar) return;
        var btns = bar.getElementsByTagName("button");
        for (var i = 0; i < btns.length; i++) {
            var b = btns[i];
            if ((b.className || "").indexOf("like-btn") < 0) continue;
            while (b.firstChild) b.removeChild(b.firstChild);
            b.appendChild(text(liked ? "♥" : "♡"));
            b.className = "like-btn" + (liked ? " liked" : "");
            b.setAttribute("title", liked ? "Убрать лайк" : "Поставить лайк");
            break;
        }
    }

    function findCommentInData(id) {
        if (!data.groups) return null;
        for (var i = 0; i < data.groups.length; i++) {
            var g = data.groups[i];
            if (!g || !g.comments) continue;
            for (var j = 0; j < g.comments.length; j++) {
                if (g.comments[j].id === id) return { c: g.comments[j], g: g, index: j };
            }
        }
        return null;
    }

    function findDirectChildByClass(parent, cls) {
        var kids = parent.children || parent.childNodes;
        for (var i = 0; i < kids.length; i++) {
            var k = kids[i];
            if (k && k.nodeType === 1 && (k.className || "") === cls) return k;
        }
        return null;
    }

    function rebuildActions(div) {
        var c = div._data;
        if (!c) return;
        var body = findDirectChildByClass(div, "body");
        var formHost = findDirectChildByClass(div, "form-host");
        if (!body || !formHost) return;

        var oldActions = findDirectChildByClass(div, "actions");
        var newActions = renderActions(c, body, formHost);
        if (oldActions) oldActions.parentNode.removeChild(oldActions);
        if (newActions) {
            var replies = findDirectChildByClass(div, "replies");
            if (replies) {
                div.insertBefore(newActions, replies);
            } else {
                div.appendChild(newActions);
            }
        }
    }

    function clearChildren(node) {
        while (node.firstChild) node.removeChild(node.firstChild);
    }

    window.__applyEdit = function (id, newText) {
        try {
            if (isFlat()) {
                var hit = findCommentInData(id);
                if (!hit) return false;
                hit.c.text = newText;

                var rowDiv = findFlatRow(id);
                if (!rowDiv) return true;

                var flatBody = findDescendantByClass(rowDiv, "flat-body");
                if (flatBody) {
                    clearChildren(flatBody);
                    appendLinkified(flatBody, newText, data.search);
                    flatBody.__originalText = newText;
                    flatBody.style.display = "";
                }

                var flatFormHost = findDescendantByClass(rowDiv, "flat-form-host");
                if (flatFormHost) clearChildren(flatFormHost);

                rebuildFlatActions(rowDiv);
                return true;
            }
            var div = findCommentDiv(id);
            if (!div || !div._data) return false;

            div._data.text = newText;

            var body = findDirectChildByClass(div, "body");
            if (body) {
                clearChildren(body);
                appendLinkified(body, newText, data.search);
                body.__originalText = newText;
                body.style.display = "";
            }

            var formHost = findDirectChildByClass(div, "form-host");
            if (formHost) clearChildren(formHost);

            rebuildActions(div);
            return true;
        } catch (e) {
            return false;
        }
    };

    window.__applyDeleted = function (id, isDeleted) {
        try {
            if (isFlat()) {
                var hit = findCommentInData(id);
                if (!hit) return false;
                hit.c.deleted = isDeleted;

                var rowDiv = findFlatRow(id);
                if (!rowDiv) return true;

                rowDiv.className = "flat-row" + (isDeleted ? " deleted" : "");

                var flatBody = findDescendantByClass(rowDiv, "flat-body");
                if (flatBody) {
                    clearChildren(flatBody);
                    if (isDeleted) {
                        flatBody.appendChild(text("[удалён]"));
                    } else {
                        appendLinkified(flatBody, flatBody.__originalText || hit.c.text || "", data.search);
                    }
                    flatBody.style.display = "";
                }

                var flatFormHost = findDescendantByClass(rowDiv, "flat-form-host");
                if (flatFormHost) clearChildren(flatFormHost);

                rebuildFlatActions(rowDiv);
                return true;
            }
            var div = findCommentDiv(id);
            if (!div || !div._data) return false;

            div._data.deleted = isDeleted;
            div.className = "comment" + (isDeleted ? " deleted" : "");

            var body = findDirectChildByClass(div, "body");
            if (body) {
                clearChildren(body);
                if (isDeleted) {
                    body.appendChild(document.createTextNode("[удалён]"));
                } else {
                    appendLinkified(body, body.__originalText || div._data.text || "", data.search);
                }
                body.style.display = "";
            }

            var formHost = findDirectChildByClass(div, "form-host");
            if (formHost) clearChildren(formHost);

            rebuildActions(div);
            return true;
        } catch (e) {
            return false;
        }
    };

    window.__applyCreate = function (json, groupKey, parentCompositeId) {
        try {
            var c = JSON.parse(json);

            if (isFlat()) {
                if (findCommentInData(c.id)) return true;
                var groups = data.groups || [];
                var target = null;
                for (var gi = 0; gi < groups.length; gi++) {
                    if (groups[gi].key === groupKey) {
                        target = groups[gi];
                        break;
                    }
                }
                if (!target) return false;
                if (!target.comments) target.comments = [];
                target.comments.unshift(c);
                target.total = (target.total || 0) + 1;

                if (parentCompositeId) {
                    var parentHit = findCommentInData(parentCompositeId);
                    if (parentHit) parentHit.c.replyCount = (parentHit.c.replyCount || 0) + 1;

                    var parentRow = findFlatRow(parentCompositeId);
                    if (!parentRow) {
                        renderFlat();
                        return true;
                    }

                    if (parentRow._row && parentRow._row.idx) {
                        parentRow._row.idx.byId[c.id] = c;
                        if (c.parent) {
                            var bucket = parentRow._row.idx.childrenByParent[c.parent];
                            if (!bucket) {
                                bucket = [];
                                parentRow._row.idx.childrenByParent[c.parent] = bucket;
                            }
                            bucket.push(c);
                        }
                    }

                    var existingReplies = null;
                    var siblings = parentRow.childNodes;
                    for (var sIdx = 0; sIdx < siblings.length; sIdx++) {
                        var sib = siblings[sIdx];
                        if (sib && sib.nodeType === 1 && (sib.className || "") === "flat-replies") {
                            existingReplies = sib;
                            break;
                        }
                    }
                    if (existingReplies) parentRow.removeChild(existingReplies);

                    rebuildFlatActions(parentRow);
                    if (parentRow.__expandReplies) parentRow.__expandReplies();
                    return true;
                }

                var list = root.getElementsByClassName("flat-list")[0];
                var emptyHost = root.getElementsByClassName("flat-empty")[0];
                if (emptyHost && emptyHost.parentNode) {
                    emptyHost.parentNode.removeChild(emptyHost);
                }
                if (!list) {
                    list = el("div", "flat-list");
                    root.appendChild(list);
                }

                var newMeta = buildFlatRowMeta(c, target);
                var newRow = renderFlatRow(newMeta, 0);
                if (list.firstChild) {
                    list.insertBefore(newRow, list.firstChild);
                } else {
                    list.appendChild(newRow);
                }
                return true;
            }

            if (findCommentDiv(c.id)) {
                return true;
            }

            var mediaNodes = document.getElementsByClassName("media");
            var mediaDiv = null;
            for (var i = 0; i < mediaNodes.length; i++) {
                if (mediaNodes[i]._groupKey === groupKey) {
                    mediaDiv = mediaNodes[i];
                    break;
                }
            }
            if (!mediaDiv) return false;

            var threads = findDirectChildByClass(mediaDiv, "threads");
            if (!threads) return false;

            var multipleSources = !!mediaDiv._multipleSources;

            if (parentCompositeId) {
                var parent = findCommentDiv(parentCompositeId);
                if (!parent) return false;

                var replies = findDirectChildByClass(parent, "replies");
                if (!replies) {
                    replies = el("div", "replies");
                    parent.appendChild(replies);
                }
                replies.appendChild(renderComment(c, {}, multipleSources));

                var parentFormHost = findDirectChildByClass(parent, "form-host");
                if (parentFormHost) clearChildren(parentFormHost);
            } else {
                threads.appendChild(renderComment(c, {}, multipleSources));

                var compose = findDirectChildByClass(mediaDiv, "compose");
                if (compose) {
                    var fh = findDirectChildByClass(compose, "form-host");
                    var trig = findDirectChildByClass(compose, "actions");
                    if (fh) clearChildren(fh);
                    if (trig) trig.style.display = "";
                }
            }

            return true;
        } catch (e) {
            return false;
        }
    };

    function findGroupDiv(groupKey) {
        var mediaNodes = document.getElementsByClassName("media");
        for (var i = 0; i < mediaNodes.length; i++) {
            if (mediaNodes[i]._groupKey === groupKey) return mediaNodes[i];
        }
        return null;
    }

    window.__applyGroup = function (groupKey, json) {
        try {
            var groupData = json ? JSON.parse(json) : null;
            if (!groupData) return false;

            var replaced = false;
            if (data.groups) {
                for (var i = 0; i < data.groups.length; i++) {
                    if (data.groups[i].key === groupKey) {
                        data.groups[i] = groupData;
                        replaced = true;
                        break;
                    }
                }
            }

            if (isFlat()) {
                if (!replaced) {
                    data.groups = data.groups || [];
                    data.groups.push(groupData);
                }
                renderFlat();
                return true;
            }

            var existing = findGroupDiv(groupKey);
            if (!existing) return false;

            var replacement = renderGroup(groupData);
            existing.parentNode.replaceChild(replacement, existing);
            return true;
        } catch (e) {
            return false;
        }
    };

    window.__applyAll = function (json) {
        try {
            var parsed = json ? JSON.parse(json) : null;
            data.search = parsed && parsed.search ? parsed.search : "";
            data.layout = parsed && parsed.layout ? parsed.layout : "grouped";
            data.sort = parsed && parsed.sort ? parsed.sort : "newest";
            data.groups = parsed && parsed.groups ? parsed.groups : [];
            render();
            return true;
        } catch (e) {
            return false;
        }
    };

    window.__applyLike = function (id, liked, count) {
        try {
            if (isFlat()) {
                var hit = findCommentInData(id);
                if (!hit) return false;
                hit.c.likedByMe = liked;
                hit.c.likes = count;

                var rowDiv = findFlatRow(id);
                if (!rowDiv) return true;
                updateFlatLikeButton(rowDiv, liked);
                updateFlatLikeCount(rowDiv, count);
                return true;
            }
            var nodes = document.getElementsByClassName("comment");
            for (var i = 0; i < nodes.length; i++) {
                var node = nodes[i];
                if (node._commentId !== id) continue;

                var directKids = node.children || node.childNodes;
                var actionsNode = null;
                var headNode = null;
                for (var k = 0; k < directKids.length; k++) {
                    var kid = directKids[k];
                    if (!kid || kid.nodeType !== 1) continue;
                    var cls = kid.className || "";
                    if (cls === "actions") actionsNode = kid;
                    else if (cls === "head") headNode = kid;
                }

                if (actionsNode) {
                    var btns = actionsNode.getElementsByTagName("button");
                    for (var j = 0; j < btns.length; j++) {
                        var b = btns[j];
                        if ((b.className || "").indexOf("like-btn") < 0) continue;
                        while (b.firstChild) b.removeChild(b.firstChild);
                        b.appendChild(document.createTextNode(liked ? "♥ Не нравится" : "♡ Нравится"));
                        b.className = "like-btn" + (liked ? " liked" : "");
                        break;
                    }
                }

                if (headNode) {
                    var existing = null;
                    var headKids = headNode.children || headNode.childNodes;
                    for (var h = 0; h < headKids.length; h++) {
                        var hk = headKids[h];
                        if (hk && hk.nodeType === 1 && (hk.className || "") === "likes") {
                            existing = hk;
                            break;
                        }
                    }
                    if (count > 0) {
                        if (!existing) {
                            existing = document.createElement("span");
                            existing.className = "likes";
                            headNode.appendChild(existing);
                        }
                        while (existing.firstChild) existing.removeChild(existing.firstChild);
                        existing.appendChild(document.createTextNode("♥ " + count));
                    } else if (existing) {
                        existing.parentNode.removeChild(existing);
                    }
                }

                return true;
            }
        } catch (e) {
        }
        return false;
    };
})();
