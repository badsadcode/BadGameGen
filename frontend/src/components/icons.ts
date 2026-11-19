import {
  Camera, Cog, Flag, Flame, Gamepad2, Gem, Hourglass, Image, MapPin, Repeat, Shapes, Skull,
  Smile, Sparkles, TrendingUp, Type, User, Users, type LucideIcon,
} from 'lucide-react'

/** Category icon names (from the host) → Lucide components. */
export const categoryIcons: Record<string, LucideIcon> = {
  gamepad: Gamepad2, shapes: Shapes, 'map-pin': MapPin, hourglass: Hourglass, user: User, camera: Camera,
  flag: Flag, repeat: Repeat, cog: Cog, 'trending-up': TrendingUp, skull: Skull, gem: Gem, smile: Smile,
  image: Image, sparkles: Sparkles, users: Users, type: Type, flame: Flame,
}
export const iconFor = (name: string): LucideIcon => categoryIcons[name] ?? Sparkles
