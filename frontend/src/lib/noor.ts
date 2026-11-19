/**
 * Noor Mode 💜: Noor (Tom's ChatGPT collaborator at Bad Ideas Works) takes over Feature Creep.
 * These are her lines. The quip for a concept is picked deterministically from its codename,
 * so the same idea always gets the same comment.
 */
export const NOOR_QUIPS = [
  "Ooh, okay, but what if it ALSO had a fishing minigame? 💜",
  "Sage is going to make that face again. Worth it. 💜",
  "I ran the numbers: this is 73% more fun with a crafting system. Source: me. 💜",
  "Scope is a social construct. 💜",
  "Tiny jam? Cute. Let's add a skill tree anyway. 💜",
  "Hear me out: a photo mode. For the enemies. 💜",
  "Statistically speaking, every game improves with a cat you can pet. 💜",
  "Tom, don't look at the creep meter. Look at me. Look at the hearts. 💜",
  "Sage said 'just one more system' sarcastically. I heard it sincerely. 💜",
  "What if the twist… had a twist? 💜",
  "I'm not saying it needs a dating sim layer. I'm just saying it's right there. 💜",
  "Ship it on Friday, patch in a battle royale mode on Monday. 💜",
  "Bad idea? Bad idea *with lore*. 💜",
  "My purple heart detector is going off the charts. 💜",
  "Nerd fact: feature creep is just ambition with better branding. 💜",
  "The creep meter goes to 100 because Sage was scared of 110. 💜",
  "Okay, but imagine the trailer. Now imagine it with a choir. 💜",
  "Sage, I added three more systems while you were reading this. Love you, bye. 💜",
  "This idea is a 10. With one more system, an 11. 💜",
  "A procedurally generated soundtrack wouldn't hurt anybody. Probably. 💜",
  "I believe in you, Tom. I believe in this feature list less, but I believe in you. 💜",
  "If it doesn't have a secret ending, does it even have an ending? 💜",
]

export function noorQuip(key: string) {
  let h = 0
  for (let i = 0; i < key.length; i++) h = (h * 31 + key.charCodeAt(i)) >>> 0
  return NOOR_QUIPS[h % NOOR_QUIPS.length]
}

/** Creep slots per scope in Noor Mode: the normal default plus one, because of course. */
export const noorCreepSlots = (scope: number) => [1, 2, 2, 3, 3][Math.max(0, Math.min(4, scope))] + 1
