(function () {
    const modal = document.getElementById('course-modal');
    const host = document.getElementById('lesson-host');
    const token = document.querySelector('#course-antiforgery input[name="__RequestVerificationToken"]')?.value;
    let currentLessonId = 0;
    let attempts = 0;

    function saveProgress(completed, score) {
        return fetch('/Course/SaveProgress', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body: JSON.stringify({ lessonId: currentLessonId, isCompleted: completed, quizScore: score || 0, quizAttempts: attempts, lastPosition: 0 })
        });
    }

    const createInitialPieces = () => {
        const pieces = [];
        const add = (row, col, type, text, color) => pieces.push({ at: row * 9 + col, type, text, color });
        const blackBack = [['Rook', '車'], ['Horse', '馬'], ['Elephant', '象'], ['Advisor', '士'], ['King', '將'], ['Advisor', '士'], ['Elephant', '象'], ['Horse', '馬'], ['Rook', '車']];
        const redBack = [['Rook', '俥'], ['Horse', '傌'], ['Elephant', '相'], ['Advisor', '仕'], ['King', '帥'], ['Advisor', '仕'], ['Elephant', '相'], ['Horse', '傌'], ['Rook', '俥']];
        blackBack.forEach(([type, text], col) => add(0, col, type, text, 'black'));
        [1, 7].forEach(col => add(2, col, 'Cannon', '炮', 'black'));
        [0, 2, 4, 6, 8].forEach(col => add(3, col, 'Pawn', '卒', 'black'));
        redBack.forEach(([type, text], col) => add(9, col, type, text, 'red'));
        [1, 7].forEach(col => add(7, col, 'Cannon', '砲', 'red'));
        [0, 2, 4, 6, 8].forEach(col => add(6, col, 'Pawn', '兵', 'red'));
        return pieces;
    };

    const lessonBoards = {
        1: { pieces: createInitialPieces() },
        3: { pieces: [{ at: 81, type: 'Rook', text: '俥', color: 'red' }, { at: 0, type: 'Rook', text: '車', color: 'black' }] },
        4: { pieces: [{ at: 73, type: 'Cannon', text: '砲', color: 'red' }, { at: 80, type: 'Pawn', text: '兵', color: 'red' }, { at: 18, type: 'Pawn', text: '卒', color: 'black' }] },
        5: { pieces: [{ at: 82, type: 'Horse', text: '傌', color: 'red' }, { at: 80, type: 'Pawn', text: '兵', color: 'red' }, { at: 18, type: 'Pawn', text: '卒', color: 'black' }] },
        10: { pieces: [{ at: 85, type: 'King', text: '帥', color: 'red' }, { at: 4, type: 'King', text: '將', color: 'black' }] },
        11: { pieces: [{ at: 84, type: 'Advisor', text: '仕', color: 'red' }, { at: 4, type: 'King', text: '將', color: 'black' }] },
        12: { pieces: [{ at: 81, type: 'Elephant', text: '相', color: 'red' }, { at: 4, type: 'King', text: '將', color: 'black' }] },
        13: { pieces: [{ at: 54, type: 'Pawn', text: '兵', color: 'red' }, { at: 31, type: 'Pawn', text: '卒', color: 'black' }] },
        14: { pieces: [{ at: 81, type: 'Rook', text: '俥', color: 'red' }, { at: 73, type: 'Cannon', text: '砲', color: 'red' }, { at: 18, type: 'Pawn', text: '卒', color: 'black' }] },
        15: { pieces: [{ at: 81, type: 'Rook', text: '俥', color: 'red' }, { at: 73, type: 'Cannon', text: '砲', color: 'red' }, { at: 82, type: 'Horse', text: '傌', color: 'red' }, { at: 18, type: 'Pawn', text: '卒', color: 'black' }, { at: 4, type: 'King', text: '將', color: 'black' }] }
    };

    function setupLessonBoard(board) {
        const visual = lessonBoards[Number(board.dataset.visualLesson)] || lessonBoards[3];
        const isIntroductionBoard = Number(board.dataset.visualLesson) === 1;
        const svg = board.querySelector('.course-board-svg');
        const piecesLayer = board.querySelector('.course-pieces-layer');
        const movesLayer = board.querySelector('.course-moves-layer');
        const point = index => ({ row: Math.floor(index / 9), col: index % 9, x: 50 + (index % 9) * 100, y: 50 + Math.floor(index / 9) * 100 });
        const key = (row, col) => row * 9 + col;
        const pieces = new Map(visual.pieces.map(piece => {
            const position = point(piece.at);
            return [key(position.row, position.col), { ...piece, row: position.row, col: position.col }];
        }));
        let selectedSource = null;
        const rendered = new Map();
        const clearSelection = () => { movesLayer.replaceChildren(); rendered.forEach(item => item.group.classList.remove('selected')); selectedSource = null; };
        const updatePiecePosition = (group, index) => {
            const position = point(index);
            const circle = group.querySelector('circle');
            const text = group.querySelector('text');
            circle.setAttribute('cx', position.x);
            circle.setAttribute('cy', position.y);
            text.setAttribute('x', position.x);
            text.setAttribute('y', position.y + 2);
        };
        const inside = (row, col) => row >= 0 && row < 10 && col >= 0 && col < 9;
        const occupied = (row, col) => pieces.get(key(row, col));
        const addIfFreeOrEnemy = (result, piece, row, col) => {
            if (!inside(row, col)) return false;
            const other = occupied(row, col);
            if (!other) result.push(key(row, col));
            else if (other.color !== piece.color) result.push(key(row, col));
            return !other;
        };
        function exposesFacingKings(piece, target) {
            const positions = new Map();
            pieces.forEach((value, position) => {
                if (position !== piece.at) positions.set(position, value);
            });
            positions.set(target, piece);
            const redKing = [...positions.entries()].find(([, value]) => value.type === 'King' && value.color === 'red');
            const blackKing = [...positions.entries()].find(([, value]) => value.type === 'King' && value.color === 'black');
            if (!redKing || !blackKing) return false;
            const redPosition = point(redKing[0]);
            const blackPosition = point(blackKing[0]);
            if (redPosition.col !== blackPosition.col) return false;
            const firstRow = Math.min(redPosition.row, blackPosition.row);
            const lastRow = Math.max(redPosition.row, blackPosition.row);
            for (let row = firstRow + 1; row < lastRow; row++) {
                if (positions.has(key(row, redPosition.col))) return false;
            }
            return true;
        }
        function legalMoves(piece) {
            const p = { row: piece.row, col: piece.col }, result = [];
            if (piece.type === 'Rook' || piece.type === 'Cannon') {
                [[1, 0], [-1, 0], [0, 1], [0, -1]].forEach(([dr, dc]) => {
                    let blocked = false, screens = 0;
                    for (let step = 1; step < 10; step++) {
                        const row = p.row + dr * step, col = p.col + dc * step;
                        if (!inside(row, col)) break;
                        const other = occupied(row, col);
                        if (piece.type === 'Rook') {
                            if (!other) result.push(key(row, col));
                            else { if (other.color !== piece.color) result.push(key(row, col)); break; }
                        } else if (!blocked) {
                            if (!other) result.push(key(row, col));
                            else { blocked = true; screens++; }
                        } else if (screens === 1) {
                            if (other) { if (other.color !== piece.color) result.push(key(row, col)); break; }
                        }
                    }
                });
            } else if (piece.type === 'Horse') {
                [[-2,-1],[-2,1],[2,-1],[2,1],[-1,-2],[-1,2],[1,-2],[1,2]].forEach(([dr, dc]) => {
                    const leg = Math.abs(dr) === 2 ? [p.row + dr / 2, p.col] : [p.row, p.col + dc / 2];
                    if (!occupied(...leg)) addIfFreeOrEnemy(result, piece, p.row + dr, p.col + dc);
                });
            } else if (piece.type === 'Elephant') {
                [[-2,-2],[-2,2],[2,-2],[2,2]].forEach(([dr, dc]) => {
                    const row = p.row + dr, col = p.col + dc;
                    if ((piece.color === 'red' ? row >= 5 : row <= 4) && !occupied(p.row + dr / 2, p.col + dc / 2)) addIfFreeOrEnemy(result, piece, row, col);
                });
            } else if (piece.type === 'Advisor') {
                [[-1,-1],[-1,1],[1,-1],[1,1]].forEach(([dr, dc]) => {
                    const row = p.row + dr, col = p.col + dc;
                    if (col >= 3 && col <= 5 && (piece.color === 'red' ? row >= 7 : row <= 2)) addIfFreeOrEnemy(result, piece, row, col);
                });
            } else if (piece.type === 'King') {
                [[-1,0],[1,0],[0,-1],[0,1]].forEach(([dr, dc]) => {
                    const row = p.row + dr, col = p.col + dc;
                    if (col >= 3 && col <= 5 && (piece.color === 'red' ? row >= 7 : row <= 2)) addIfFreeOrEnemy(result, piece, row, col);
                });
            } else if (piece.type === 'Pawn') {
                const forward = piece.color === 'red' ? -1 : 1;
                addIfFreeOrEnemy(result, piece, p.row + forward, p.col);
                if (piece.color === 'red' ? p.row <= 4 : p.row >= 5) {
                    addIfFreeOrEnemy(result, piece, p.row, p.col - 1);
                    addIfFreeOrEnemy(result, piece, p.row, p.col + 1);
                }
            }
            return result.filter(target => !exposesFacingKings(piece, target));
        }
        visual.pieces.forEach(piece => {
            const p = point(piece.at);
            const statePiece = pieces.get(piece.at);
            const group = document.createElementNS('http://www.w3.org/2000/svg', 'g');
            group.classList.add('course-piece', piece.color);
            group.dataset.at = statePiece.at;
            group.dataset.row = statePiece.row;
            group.dataset.col = statePiece.col;
            group.innerHTML = `<circle cx="${p.x}" cy="${p.y}" r="42"></circle><text x="${p.x}" y="${p.y + 2}">${piece.text}</text>`;
            group.setAttribute('pointer-events', isIntroductionBoard ? 'none' : 'all');
            if (isIntroductionBoard) {
                piecesLayer.appendChild(group);
                rendered.set(piece.at, { group, piece: statePiece, position: piece.at });
                return;
            }
            group.addEventListener('click', event => {
                event.stopPropagation();
                const at = Number(group.dataset.at);
                const currentPiece = pieces.get(at);
                if (!currentPiece) return;
                if (selectedSource && selectedSource.at !== at && legalMoves(selectedSource.piece).includes(at) && selectedSource.piece.color !== currentPiece.color) {
                    movePiece(selectedSource.at, at);
                    return;
                }
                if (currentPiece.color !== 'red') return;
                movesLayer.replaceChildren();
                rendered.forEach(item => item.group.classList.remove('selected'));
                group.classList.add('selected');
                selectedSource = { at, piece: currentPiece, group };
                legalMoves(currentPiece).forEach(target => {
                    const destination = point(target);
                    const move = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
                    move.classList.add('course-move');
                    move.setAttribute('cx', destination.x); move.setAttribute('cy', destination.y); move.setAttribute('r', '13');
                    move.addEventListener('click', () => movePiece(at, target));
                    movesLayer.appendChild(move);
                });
            });
            piecesLayer.appendChild(group);
            rendered.set(piece.at, { group, piece: statePiece, position: piece.at });
        });
        function movePiece(from, to) {
            const item = rendered.get(from), moving = pieces.get(from);
            if (!item || !moving) return;
            if (!legalMoves(moving).includes(to)) {
                if (exposesFacingKings(moving, to)) {
                    showIllegalMove('Nước đi không hợp lệ: làm lộ mặt hai Tướng!');
                }
                return;
            }
            const captured = rendered.get(to);
            if (captured) { captured.group.remove(); rendered.delete(to); }
            updatePiecePosition(item.group, to);
            item.group.dataset.at = to;
            const destination = point(to);
            moving.at = to;
            moving.row = destination.row;
            moving.col = destination.col;
            item.position = to;
            item.group.dataset.row = destination.row;
            item.group.dataset.col = destination.col;
            pieces.delete(from); pieces.set(to, moving);
            rendered.delete(from); rendered.set(to, item);
            clearSelection();
            if (![...pieces.values()].some(piece => piece.color === 'black')) {
                const notice = document.createElementNS('http://www.w3.org/2000/svg', 'text');
                notice.classList.add('course-game-over');
                notice.setAttribute('x', '450');
                notice.setAttribute('y', '510');
                notice.textContent = 'Đã ăn hết quân Đen - Kết thúc!';
                svg.appendChild(notice);
                piecesLayer.style.pointerEvents = 'none';
                movesLayer.style.pointerEvents = 'none';
            }
        }
        function showIllegalMove(message) {
            let notice = board.querySelector('.course-illegal-move');
            if (!notice) {
                notice = document.createElementNS('http://www.w3.org/2000/svg', 'text');
                notice.classList.add('course-illegal-move');
                notice.setAttribute('x', '450');
                notice.setAttribute('y', '520');
                svg.appendChild(notice);
            }
            notice.textContent = message;
            window.clearTimeout(notice.hideTimer);
            notice.hideTimer = window.setTimeout(() => notice.remove(), 2200);
        }
        svg.addEventListener('click', event => { if (event.target === svg) clearSelection(); });
    }

    const moduleButtons = [...document.querySelectorAll('.module-summary')];
    const modules = [...document.querySelectorAll('.course-module')];
    function selectModule(index) {
        moduleButtons.forEach((button, buttonIndex) => {
            const active = buttonIndex === index;
            button.classList.toggle('active', active);
            button.setAttribute('aria-pressed', active ? 'true' : 'false');
        });
        modules.forEach((module, moduleIndex) => { module.hidden = moduleIndex !== index; });
    }
    moduleButtons.forEach((button, index) => button.addEventListener('click', () => selectModule(index)));
    if (moduleButtons.length) selectModule(0);

    document.querySelectorAll('.lesson-open').forEach(button => button.addEventListener('click', () => {
        currentLessonId = Number(button.dataset.lessonId);
        const template = document.getElementById('lesson-data-' + currentLessonId);
        host.replaceChildren(template.content.cloneNode(true));
        modal.classList.add('show');
        modal.setAttribute('aria-hidden', 'false');

        host.querySelectorAll('.course-board').forEach(setupLessonBoard);

        host.querySelectorAll('.btn-board-reset').forEach(btn => btn.addEventListener('click', () => {
            const board = host.querySelector('.course-board');
            if (board) setupLessonBoard(board);
        }));

        host.querySelectorAll('.btn-board-copy-fen').forEach(btn => btn.addEventListener('click', () => {
            const visual = lessonBoards[currentLessonId];
            if (navigator.clipboard) {
                const fenText = currentLessonId === 1 
                    ? 'rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1' 
                    : 'Thế cờ bài học #' + currentLessonId;
                navigator.clipboard.writeText(fenText).then(() => {
                    btn.innerHTML = '<i class="fa-solid fa-check"></i> Đã chép!';
                    setTimeout(() => btn.innerHTML = '<i class="fa-regular fa-copy"></i> Sao chép FEN', 1800);
                });
            }
        }));

        host.querySelectorAll('.quiz-options button').forEach(option => option.addEventListener('click', () => {
            option.parentElement.querySelectorAll('button').forEach(item => item.classList.remove('selected'));
            option.classList.add('selected');
        }));
        host.querySelector('.quiz-submit')?.addEventListener('click', gradeQuiz);
    }));

    function gradeQuiz() {
        attempts++;
        let correct = 0;
        const questions = host.querySelectorAll('.quiz-question');
        questions.forEach(question => {
            const answer = question.querySelector('.selected');
            const isCorrect = answer && Number(answer.dataset.option) === Number(question.dataset.answer);
            if (isCorrect) correct++;
            question.classList.add('answered');
            question.querySelectorAll('button').forEach(option => {
                option.classList.toggle('correct', Number(option.dataset.option) === Number(question.dataset.answer));
                option.classList.toggle('wrong', option.classList.contains('selected') && !isCorrect);
            });
        });
        const score = questions.length ? Math.round(correct * 100 / questions.length) : 0;
        const result = host.querySelector('.quiz-result');
        if (result) { result.textContent = `Kết quả: ${correct}/${questions.length} (${score}%)`; result.dataset.score = score; }
        saveProgress(score === 100, score).then(async response => {
            if (score === 100 && response.ok) {
                const data = await response.json().catch(() => ({}));
                if (data.isGuest) {
                    result.textContent += ' - Xuất sắc! (Đăng nhập để lưu huy hiệu này)';
                    result.classList.add('success');
                } else {
                    result.textContent += ' - Bài học đã hoàn thành!';
                    result.classList.add('success');
                    setTimeout(() => window.location.reload(), 900);
                }
            }
        });
    }

    document.querySelector('.modal-close')?.addEventListener('click', close);
    modal?.addEventListener('click', event => { if (event.target === modal) close(); });
    function close() { modal.classList.remove('show'); modal.setAttribute('aria-hidden', 'true'); host.replaceChildren(); }
})();
