using System.Collections.Generic;

namespace SuperNova.Runtime.Interpreter;

public interface ICSharpProxy
{
    void Call(string method, List<Vb6Value> args);
}