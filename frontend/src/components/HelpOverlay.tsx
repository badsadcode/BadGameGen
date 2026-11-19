const SHORTCUTS: [string, string][] = [
  ['G / Space', 'Generate Trouble (new seed, keeps locks)'],
  ['R', 'Reroll every unlocked field'],
  ['W', 'Reword (same ingredients, new sentences)'],
  ['T', 'New title'],
  ['F', 'Toggle Feature Creep Mode'],
  ['A', 'And another thing...'],
  ['S', 'Save to Vault'],
  ['C', 'Copy Markdown'],
  ['1 / 2 / 3', 'One-liner / Short pitch / Expanded'],
  ['Alt + ← / →', 'Previous / next idea'],
  ['?', 'This help'],
]

export function HelpOverlay({ onClose }: { onClose: () => void }) {
  return (
    <div className="modal-backdrop" onMouseDown={onClose}>
      <div className="modal help" role="dialog" aria-modal="true" aria-label="Keyboard shortcuts" onMouseDown={(e) => e.stopPropagation()}>
        <h3>Keyboard shortcuts</h3>
        <table>
          <tbody>{SHORTCUTS.map(([k, d]) => <tr key={k}><td><kbd>{k}</kbd></td><td>{d}</td></tr>)}</tbody>
        </table>
        <p className="muted small">Shortcuts are ignored while typing in a text field.</p>
        <div className="modal-actions"><button className="btn primary" onClick={onClose} autoFocus>Got it</button></div>
      </div>
    </div>
  )
}
