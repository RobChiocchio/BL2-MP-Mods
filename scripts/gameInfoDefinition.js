const BORDERLANDS = 0;
const BORDERLANDS2 = 2;
const BORDERLANDSTPS = 3;

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
 