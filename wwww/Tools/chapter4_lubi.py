"""Whole-line Mandarin readings for Lubi. Punctuation directs pauses in the recording."""
VOICE = 'zh-TW-HsiaoChenNeural'
REVISION = 'cave_partner_neural_v2'

# Text shown on screen remains the dialogue key. Keep every spoken word.
READINGS = {
    '古慕，靠著我。慢慢起來，我扶你一起走。': ('古慕，靠著我。慢慢起來……我扶你一起走。', -1, 1),
    '古慕，坐穩了再鬆手。我在你旁邊，你先慢慢把氣喘勻。': ('古慕，坐穩了再鬆手。我在你旁邊。你先慢慢把氣喘勻。', -4, 0),
    '古慕，看見那個洞口了嗎？再靠過來一點，我陪你走進去。': ('古慕，看見那個洞口了嗎？再靠過來一點，我陪你走進去。', 1, 2),
    '古慕，把重量靠過來。腳下有突出的石頭，我們一步一步跨過去，不用趕著追。': ('古慕，把重量靠過來。腳下有突出的石頭，我們一步一步跨過去。不用趕著追。', 0, 1),
    '這一段終於走過來了。古慕，先站穩，抓好我的肩，我不會把你留在這裡。': ('這一段終於走過來了。古慕，先站穩。抓好我的肩，我不會把你留在這裡。', -2, 0),
}
