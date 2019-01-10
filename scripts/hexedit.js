const _ = require('lodash');
const Promise = require("bluebird");

if (require.main === module) { // If not just an import
    standalone();
}

/**
 * @param {Array.<Object>} patches The patch objects to apply
 * @param {WriteStream} fileStream The fileStream to apply the patch
 */
export default function edit(patches, fileStream) { // Check list of mods and apply modName to filePath // (or something like that) TODO
    

} // TODO: return success

function standalone() { // TODO: do standalone script
    var meow = require("meow");

    // Process arguments
    let args = meow("\
        Usage\
            $ hexedit <file> <mod>\
\
        Examples\
            $ hexedit \"WillowGame\\CookedPCConsole\\WillowGame.upk\" debugMenu\
    ");

    //edit
}