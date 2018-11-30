const game = {
    bl1: {
        id: 0,
        executablePath: "\\Binaries\\Borderlands.exe",
        gameDirectoryName: "Borderlands",
        contentDirectoryRelativePath: "\\WillowGame\\CookedPC\\",
        packageExtension: ".u",
        patchFileName: "cooppatch_bl1.txt"
    },
    bl2: {
        id: 1,
        executablePath: "\\Binaries\\Win32\\Borderlands2.exe",
        gameDirectoryName: "Borderlands 2",
        contentDirectoryRelativePath: "\\WillowGame\\CookedPCConsole\\",
        packageExtension: ".upk",
        patchFileName: "cooppatch.txt"
    },
    bltps: {
        id: 2,
        executablePath: "\\Binaries\\Win32\\BorderlandsPreSequel.exe",
        gameDirectoryName: "BorderlandsPreSequel",
        contentDirectoryRelativePath: "\\WillowGame\\CookedPCConsole\\",
        packageExtension: ".upk",
        patchFileName: "cooppatch.txt"
    }
};

/* 
let gameInfo = class {
    constructor(game, os) {
        this.game = game;
        this.os = os;

        switch(game) {
            case BORDERLANDS:
                break;
            case BORDERLANDS2:
                break;
            case BORDERLANDSTPS:
                break;
            default: //throw error
                throw "InvalidGame";
                break;
        }
    }
}
  */