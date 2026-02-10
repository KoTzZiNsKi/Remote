package com.remotemouse.app

object CharToVKey {
    private val map = buildMap()
    fun vkeyFor(c: Char): Int? = map[c]
    private fun buildMap(): Map<Char, Int> {
        val m = mutableMapOf<Char, Int>()
        for (i in 0..9) m['0' + i] = VKey.NUM_0 + i
        m[' '] = VKey.SPACE
        m['\n'] = VKey.RETURN
        "qwertyuiop".forEach { ch ->
            m[ch] = 0x41 + (ch - 'a')
            m[ch.uppercaseChar()] = 0x41 + (ch - 'a')
        }
        "asdfghjkl".forEach { ch ->
            m[ch] = 0x41 + (ch - 'a')
            m[ch.uppercaseChar()] = 0x41 + (ch - 'a')
        }
        "zxcvbnm".forEach { ch ->
            m[ch] = 0x41 + (ch - 'a')
            m[ch.uppercaseChar()] = 0x41 + (ch - 'a')
        }
        val ruVk = mapOf(
            'й' to 0x51, 'ц' to 0x57, 'у' to 0x45, 'к' to 0x52, 'е' to 0x54, 'н' to 0x59, 'г' to 0x55,
            'ш' to 0x49, 'щ' to 0x4F, 'з' to 0x50, 'х' to VKey.OEM_4, 'ъ' to VKey.OEM_6,
            'ф' to 0x41, 'ы' to 0x53, 'в' to 0x44, 'а' to 0x46, 'п' to 0x47, 'р' to 0x48, 'о' to 0x4A,
            'л' to 0x4B, 'д' to 0x4C, 'ж' to VKey.OEM_1, 'э' to VKey.OEM_7,
            'я' to 0x5A, 'ч' to 0x58, 'с' to 0x43, 'м' to 0x56, 'и' to 0x42, 'т' to 0x4E, 'ь' to 0x4D,
            'б' to VKey.OEM_COMMA, 'ю' to VKey.OEM_PERIOD
        )
        ruVk.forEach { (ru, vk) ->
            m[ru] = vk
            m[ru.uppercaseChar()] = vk
        }
        return m
    }
}
