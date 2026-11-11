// Cook mode: slide navigation, prep checklist, screen wake lock and multiple step timers.
(() => {
    const root = document.getElementById('cook');
    if (!root) {
        return;
    }

    const recipeId = root.dataset.recipeId;
    const keys = {
        slide: `souschef:${recipeId}:slide`,
        checks: `souschef:${recipeId}:checks`,
        timers: `souschef:${recipeId}:timers`
    };
    const slides = [...root.querySelectorAll('.sc-slide')];
    const dots = [...root.querySelectorAll('.sc-dot')];
    const prev = document.getElementById('cook-prev');
    const next = document.getElementById('cook-next');
    const progress = document.getElementById('cook-progress');
    const tray = document.getElementById('timer-tray');

    const load = (key, fallback) => {
        try {
            return JSON.parse(localStorage.getItem(key)) ?? fallback;
        } catch {
            return fallback;
        }
    };
    const save = (key, value) => localStorage.setItem(key, JSON.stringify(value));

    // Slides.
    let current = Math.min(Math.max(load(keys.slide, 0), 0), slides.length - 1);

    function show(index) {
        current = Math.min(Math.max(index, 0), slides.length - 1);
        slides.forEach((slide, i) => slide.classList.toggle('active', i === current));
        dots.forEach((dot, i) => dot.classList.toggle('active', i === current));
        prev.disabled = current === 0;
        next.disabled = current === slides.length - 1;
        next.textContent = current === 0 ? 'Start ▶' : 'Next ▶';
        progress.textContent = slides[current].dataset.title ?? '';
        save(keys.slide, current);
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    prev.addEventListener('click', () => show(current - 1));
    next.addEventListener('click', () => show(current + 1));
    dots.forEach(dot => dot.addEventListener('click', () => show(Number(dot.dataset.index))));
    document.addEventListener('keydown', e => {
        if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) {
            return;
        }
        if (e.key === 'ArrowRight') {
            show(current + 1);
        } else if (e.key === 'ArrowLeft') {
            show(current - 1);
        }
    });

    let touchX = null;
    let touchY = null;
    root.addEventListener('touchstart', e => {
        touchX = e.touches[0].clientX;
        touchY = e.touches[0].clientY;
    }, { passive: true });
    root.addEventListener('touchend', e => {
        if (touchX === null || e.target.closest('.sc-timer-tray')) {
            return;
        }
        const dx = e.changedTouches[0].clientX - touchX;
        const dy = e.changedTouches[0].clientY - touchY;
        if (Math.abs(dx) > 70 && Math.abs(dy) < 50) {
            show(current + (dx < 0 ? 1 : -1));
        }
        touchX = null;
    }, { passive: true });

    // Prep checklist.
    const checks = load(keys.checks, {});
    root.querySelectorAll('.sc-check').forEach(box => {
        box.checked = !!checks[box.dataset.key];
        box.addEventListener('change', () => {
            checks[box.dataset.key] = box.checked;
            save(keys.checks, checks);
        });
    });

    // Keep the screen on while cooking.
    let wakeLock = null;
    async function requestWakeLock() {
        try {
            if ('wakeLock' in navigator && document.visibilityState === 'visible') {
                wakeLock = await navigator.wakeLock.request('screen');
            }
        } catch {
            wakeLock = null;
        }
    }
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') {
            requestWakeLock();
        }
    });
    requestWakeLock();

    // Alerts: a chime, a vibration and (when the page is in the background) a notification.
    let audio = null;
    function unlockAudio() {
        audio ??= new (window.AudioContext || window.webkitAudioContext)();
        if (audio.state === 'suspended') {
            audio.resume();
        }
    }
    function chime(count) {
        if (!audio) {
            return;
        }
        for (let i = 0; i < count; i++) {
            const start = audio.currentTime + i * 0.35;
            const osc = audio.createOscillator();
            const gain = audio.createGain();
            osc.frequency.value = 880;
            gain.gain.setValueAtTime(0.0001, start);
            gain.gain.exponentialRampToValueAtTime(0.4, start + 0.02);
            gain.gain.exponentialRampToValueAtTime(0.0001, start + 0.3);
            osc.connect(gain).connect(audio.destination);
            osc.start(start);
            osc.stop(start + 0.32);
        }
    }
    function alertUser(message, count) {
        chime(count);
        navigator.vibrate?.([300, 150, 300]);
        if ('Notification' in window && Notification.permission === 'granted' && document.visibilityState !== 'visible') {
            new Notification('SousChef', { body: message, tag: message });
        }
    }

    // Timers: elapsed time is stored rather than counted, so they survive reloads and sleeping tabs.
    let timers = load(keys.timers, []);
    const persist = () => save(keys.timers, timers);
    const elapsed = timer => timer.elapsedBefore + (timer.runningSince ? Date.now() - timer.runningSince : 0);

    function format(ms) {
        const total = Math.max(0, Math.round(ms / 1000));
        const hours = Math.floor(total / 3600);
        const minutes = Math.floor((total % 3600) / 60);
        const seconds = String(total % 60).padStart(2, '0');
        return hours > 0 ? `${hours}:${String(minutes).padStart(2, '0')}:${seconds}` : `${minutes}:${seconds}`;
    }

    function startTimer(label, min, max) {
        unlockAudio();
        if ('Notification' in window && Notification.permission === 'default') {
            Notification.requestPermission();
        }
        timers.push({ id: `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`, label, min, max, elapsedBefore: 0, runningSince: Date.now(), checked: false, done: false, lastRing: 0 });
        persist();
        render();
    }

    function act(id, action) {
        const timer = timers.find(t => t.id === id);
        if (!timer) {
            return;
        }
        unlockAudio();
        if (action === 'pause') {
            timer.elapsedBefore = elapsed(timer);
            timer.runningSince = null;
        } else if (action === 'resume') {
            timer.runningSince = Date.now();
        } else if (action === 'add') {
            timer.min += 60;
            timer.max += 60;
            timer.done = false;
            timer.checked = elapsed(timer) >= timer.min * 1000;
        } else if (action === 'remove') {
            timers = timers.filter(t => t.id !== id);
        }
        persist();
        render();
    }

    function render() {
        tray.replaceChildren(...timers.map(timer => {
            const ms = elapsed(timer);
            const minMs = timer.min * 1000;
            const maxMs = timer.max * 1000;
            const isRange = timer.max > timer.min;
            let state = 'running';
            let text = format(minMs - ms);
            if (ms >= maxMs) {
                state = 'done';
                text = `Done · +${format(ms - maxMs)}`;
            } else if (isRange && ms >= minMs) {
                state = 'check';
                text = `Check · up to ${format(maxMs - ms)}`;
            } else if (isRange) {
                text = `${format(minMs - ms)} – ${format(maxMs - ms)}`;
            }
            const item = document.createElement('div');
            item.className = `sc-timer sc-timer-${state}${timer.runningSince ? '' : ' paused'}`;
            item.innerHTML = `
                <div class="sc-timer-bar" style="width:${Math.min(100, (ms / maxMs) * 100)}%"></div>
                <div class="sc-timer-label"></div>
                <div class="sc-timer-time">${text}</div>
                <div class="sc-timer-actions">
                    <button type="button" data-action="${timer.runningSince ? 'pause' : 'resume'}" title="${timer.runningSince ? 'Pause' : 'Resume'}">${timer.runningSince ? '❚❚' : '▶'}</button>
                    <button type="button" data-action="add" title="Add a minute">+1</button>
                    <button type="button" data-action="remove" title="${state === 'done' ? 'Dismiss' : 'Cancel'}">✕</button>
                </div>`;
            item.querySelector('.sc-timer-label').textContent = timer.label;
            item.querySelectorAll('button').forEach(button => button.addEventListener('click', () => act(timer.id, button.dataset.action)));
            return item;
        }));
        root.querySelectorAll('.sc-timer-chip').forEach(chip => {
            chip.classList.toggle('running', timers.some(t => t.label === chip.dataset.label && t.min === Number(chip.dataset.min)));
        });
        document.body.classList.toggle('sc-has-timers', timers.length > 0);
    }

    function tick() {
        let changed = false;
        for (const timer of timers) {
            const ms = elapsed(timer);
            if (!timer.checked && timer.max > timer.min && ms >= timer.min * 1000) {
                timer.checked = true;
                changed = true;
                alertUser(`${timer.label}: check it, up to ${format(timer.max * 1000 - ms)} more.`, 2);
            }
            if (ms >= timer.max * 1000) {
                if (!timer.done || Date.now() - timer.lastRing > 15000) {
                    alertUser(`${timer.label}: time's up!`, 3);
                    timer.done = true;
                    timer.checked = true;
                    timer.lastRing = Date.now();
                    changed = true;
                }
            }
        }
        if (changed) {
            persist();
        }
        render();
    }

    root.addEventListener('click', e => {
        const chip = e.target.closest('.sc-timer-chip');
        if (chip) {
            startTimer(chip.dataset.label, Number(chip.dataset.min), Number(chip.dataset.max));
        }
    });
    document.addEventListener('pointerdown', unlockAudio, { once: true });

    // Finishing clears this recipe's saved progress.
    document.getElementById('cooked-form')?.addEventListener('submit', () => {
        Object.values(keys).forEach(key => localStorage.removeItem(key));
    });

    show(current);
    render();
    setInterval(tick, 500);
})();
