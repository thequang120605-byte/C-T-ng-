import json
import os

os.makedirs('cotuong_engine/lib/src/search', exist_ok=True)
with open('Checks/ground_truth_suite.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

black_turn = data['ZobristBlackTurnKeyHex']
piece_keys = data['ZobristPieceKeysHex']

out = [
    '// Generated Zobrist Keys from ground truth',
    f'final BigInt zobristBlackTurnKey = BigInt.parse("0x{black_turn}");',
    'final List<BigInt> zobristPieceKeys = ['
]
for k in piece_keys:
    out.append(f'  BigInt.parse("0x{k}"),')
out.append('];')

with open('cotuong_engine/lib/src/search/zobrist_keys.dart', 'w', encoding='utf-8') as f:
    f.write('\n'.join(out))
print('Generated zobrist_keys.dart successfully.')
