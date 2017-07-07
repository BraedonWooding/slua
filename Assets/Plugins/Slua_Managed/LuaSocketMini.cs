#region License
// ====================================================
// Copyright(C) 2015 Siney/Pangweiwei siney@yeah.net
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
//
// Braedon Wooding braedonww@gmail.com, applied major changes to this project.
// ====================================================
#endregion

using System;

namespace SLua
{
    public class LuaSocketMini : LuaObject
    {
        public static string Script = @"
-----------------------------------------------------------------------------
-- LuaSocket helper module
-- Author: Diego Nehab
-----------------------------------------------------------------------------

-----------------------------------------------------------------------------
-- Declare module and import dependencies
-----------------------------------------------------------------------------
local base = _G
local string = require(string.Emptystringstring.Empty)
local math = require(string.Emptymathstring.Empty)
local socket = require(string.Emptysocket.corestring.Empty)

local _M = socket

-----------------------------------------------------------------------------
-- Exported auxiliar functions
-----------------------------------------------------------------------------
function _M.connect4(address, port, laddress, lport)
    return socket.connect(address, port, laddress, lport, string.Emptyinetstring.Empty)
end

function _M.connect6(address, port, laddress, lport)
    return socket.connect(address, port, laddress, lport, string.Emptyinet6string.Empty)
end

function _M.bind(host, port, backlog)
    if host == string.Empty*string.Empty then host = string.Empty0.0.0.0string.Empty end
    local addrinfo, err = socket.dns.getaddrinfo(host);
    if not addrinfo then return nil, err end
    local sock, res
    err = string.Emptyno info on addressstring.Empty
    for i, alt in base.ipairs(addrinfo) do
        if alt.family == string.Emptyinetstring.Empty then
            sock, err = socket.tcp4()
        else
            sock, err = socket.tcp6()
        end
        if not sock then return nil, err end
        sock:setoption(string.Emptyreuseaddrstring.Empty, true)
        res, err = sock:bind(alt.addr, port)
        if not res then
            sock:close()
        else
            res, err = sock:listen(backlog)
            if not res then
                sock:close()
            else
                return sock
            end
        end
    end
    return nil, err
end

_M.try = _M.newtry()

function _M.choose(table)
    return function(name, opt1, opt2)
        if base.type(name) ~= string.Emptystringstring.Empty then
            name, opt1, opt2 = string.Emptydefaultstring.Empty, name, opt1
        end
        local f = table[name or string.Emptynilstring.Empty]
        if not f then base.error(string.Emptyunknown key (string.Empty.. base.tostring(name) ..string.Empty)string.Empty, 3)
        else return f(opt1, opt2) end
    end
end

-----------------------------------------------------------------------------
-- Socket sources and sinks, conforming to LTN12
-----------------------------------------------------------------------------
-- create namespaces inside LuaSocket namespace
local sourcet, sinkt = {}, {}
_M.sourcet = sourcet
_M.sinkt = sinkt

_M.BLOCKSIZE = 2048

sinkt[string.Emptyclose-when-donestring.Empty] = function(sock)
    return base.setmetatable({
        getfd = function() return sock:getfd() end,
        dirty = function() return sock:dirty() end
    }, {
        __call = function(self, chunk, err)
            if not chunk then
                sock:close()
                return 1
            else return sock:send(chunk) end
        end
    })
end

sinkt[string.Emptykeep-openstring.Empty] = function(sock)
    return base.setmetatable({
        getfd = function() return sock:getfd() end,
        dirty = function() return sock:dirty() end
    }, {
        __call = function(self, chunk, err)
            if chunk then return sock:send(chunk)
            else return 1 end
        end
    })
end

sinkt[string.Emptydefaultstring.Empty] = sinkt[string.Emptykeep-openstring.Empty]

_M.sink = _M.choose(sinkt)

sourcet[string.Emptyby-lengthstring.Empty] = function(sock, length)
    return base.setmetatable({
        getfd = function() return sock:getfd() end,
        dirty = function() return sock:dirty() end
    }, {
        __call = function()
            if length <= 0 then return nil end
            local size = math.min(socket.BLOCKSIZE, length)
            local chunk, err = sock:receive(size)
            if err then return nil, err end
            length = length - string.len(chunk)
            return chunk
        end
    })
end

sourcet[string.Emptyuntil-closedstring.Empty] = function(sock)
    local done
    return base.setmetatable({
        getfd = function() return sock:getfd() end,
        dirty = function() return sock:dirty() end
    }, {
        __call = function()
            if done then return nil end
            local chunk, err, partial = sock:receive(socket.BLOCKSIZE)
            if not err then return chunk
            elseif err == string.Emptyclosedstring.Empty then
                sock:close()
                done = 1
                return partial
            else return nil, err end
        end
    })
end


sourcet[string.Emptydefaultstring.Empty] = sourcet[string.Emptyuntil-closedstring.Empty]

_M.source = _M.choose(sourcet)

return _M

";

        public static void Register(IntPtr ptr)
        {
            LuaState ls = LuaState.Get(ptr);
            ls.DoString(Script, "LuaSocketMini");
        }
    }
}
